// <data-table> client: keeps the table state (page, pageSize, sort, dir, q + filter fields) in the
// query string, fetches the server-rendered body fragment (rows + pager) from the table's source
// URL with an X-DataTable header, swaps it in and — when the tag helper marked the table with
// data-dt-sync-url (it refreshes from the page's own path) — mirrors the URL with
// history.replaceState so a reload/back shows the same view. The first render is server-side;
// this only handles changes. After a swap, focus returns to the equivalent control (sort header,
// pager link, page input) and a visually hidden live region announces the new range.

import { fetchHtml } from './http.js';

const SEARCH_DEBOUNCE_MS = 400;
const STATE_KEYS = { page: 'page', pageSize: 'pageSize', search: 'q' };

class DataTable {
    constructor(root) {
        this.root = root;
        this.id = root.id;
        this.source = root.dataset.dtSource || window.location.pathname;
        this.syncUrl = root.dataset.dtSyncUrl === 'true';
        this.body = root.querySelector('[data-dt-body]');
        this.status = root.querySelector('[data-dt-status]');
        this.search = root.querySelector('[data-dt-search]');
        this.params = new URLSearchParams(window.location.search);
        this.hiddenColumns = new Set();
        this.controller = null;
        this.searchTimer = null;
        this.bind();
    }

    bind() {
        this.search?.addEventListener('input', () => {
            this.clearSearchTimer();
            this.searchTimer = setTimeout(() => {
                this.searchTimer = null;
                this.update({ [STATE_KEYS.search]: this.search.value.trim() });
            }, SEARCH_DEBOUNCE_MS);
        });

        // Text filters apply on Enter and again on change (blur) — update() skips the second one
        // when the value did not change.
        const filters = this.root.querySelector('[data-dt-filters]');
        filters?.addEventListener('change', (event) => {
            const field = event.target;
            if (field instanceof HTMLInputElement || field instanceof HTMLSelectElement) {
                if (field.name) this.update({ [field.name]: field.value.trim() });
            }
        });
        filters?.addEventListener('keydown', (event) => {
            const field = event.target;
            if (event.key === 'Enter' && field instanceof HTMLInputElement && field.name) {
                event.preventDefault();
                this.update({ [field.name]: field.value.trim() });
            }
        });

        this.root.addEventListener('click', (event) => {
            const link = event.target.closest('a[data-dt-nav]');
            if (link && this.root.contains(link)) {
                event.preventDefault();
                if (link.closest('.disabled')) return;
                this.navigate(link);
                return;
            }

            if (event.target.closest('[data-dt-refresh]')) {
                event.preventDefault();
                this.load();
                return;
            }

            if (event.target.closest('[data-dt-export]')) {
                event.preventDefault();
                this.exportCsv();
            }
        });

        this.root.addEventListener('change', (event) => {
            const target = event.target;
            if (target.matches('[data-dt-page-size]')) {
                this.update({ [STATE_KEYS.pageSize]: target.value });
            } else if (target.matches('[data-dt-page-input]')) {
                this.goToPage(target);
            } else if (target.matches('[data-dt-toggle-col]')) {
                const key = target.dataset.dtToggleCol;
                if (target.checked) this.hiddenColumns.delete(key);
                else this.hiddenColumns.add(key);
                this.applyHiddenColumns();
            }
        });

        this.root.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' && event.target.matches('[data-dt-page-input]')) {
                event.preventDefault();
                this.goToPage(event.target);
            }
        });

        // Other scripts (e.g. notifications.js on a push) can ask tables to reload.
        document.addEventListener('dt:refresh', (event) => {
            if (!event.detail?.id || event.detail.id === this.id) this.load();
        });
    }

    clearSearchTimer() {
        const pending = this.searchTimer !== null;
        clearTimeout(this.searchTimer);
        this.searchTimer = null;
        return pending;
    }

    /** Sort header / pager link: its href carries the complete next state. */
    navigate(link) {
        const params = new URL(link.href, window.location.href).searchParams;

        // A search still waiting for its debounce would otherwise fire after this navigation and
        // undo it; fold the typed term into this request instead (a new term means a new page 1).
        if (this.clearSearchTimer() && this.search) {
            const term = this.search.value.trim();
            if ((params.get(STATE_KEYS.search) ?? '') !== term) {
                if (term) params.set(STATE_KEYS.search, term);
                else params.delete(STATE_KEYS.search);
                params.delete(STATE_KEYS.page);
            }
        }

        this.params = params;
        this.load();
    }

    goToPage(input) {
        const max = Number(input.max) || 1;
        const page = Math.min(Math.max(Math.trunc(Number(input.value)) || 1, 1), max);
        input.value = String(page);
        this.update({ [STATE_KEYS.page]: page === 1 ? '' : String(page) }, false);
    }

    /**
     * Applies state changes and reloads — unless nothing actually changed (e.g. Enter followed by
     * the change event on blur). Any change other than the page itself goes back to page 1.
     */
    update(changes, resetPage = true) {
        const entries = Object.entries(changes);
        const changed = entries.some(([key, value]) => (this.params.get(key) ?? '') !== (value ?? ''));
        if (!changed) return;

        for (const [key, value] of entries) {
            if (value) this.params.set(key, value);
            else this.params.delete(key);
        }
        if (resetPage && !(STATE_KEYS.page in changes)) this.params.delete(STATE_KEYS.page);
        if (this.params.get(STATE_KEYS.page) === '1') this.params.delete(STATE_KEYS.page);
        this.load();
    }

    url() {
        const query = this.params.toString();
        return query ? `${this.source}?${query}` : this.source;
    }

    setBusy(busy) {
        this.root.setAttribute('aria-busy', busy ? 'true' : 'false');
        this.body.classList.toggle('dt-busy', busy);
        this.root.querySelector('[data-dt-refresh] .bi')?.classList.toggle('dt-spin', busy);
    }

    /** Describes the focused control inside the body so an equivalent one can be focused after the swap. */
    captureFocus() {
        const active = document.activeElement;
        if (!(active instanceof HTMLElement) || !this.body.contains(active)) return null;

        const header = active.closest('th[data-col]');
        if (header) return { selector: `th[data-col="${CSS.escape(header.dataset.col)}"] [data-dt-nav]` };
        if (active.matches('[data-dt-page-input]')) return { selector: '[data-dt-page-input]' };
        if (active.matches('[data-dt-page-size]')) return { selector: '[data-dt-page-size]' };

        const label = active.matches('a[data-dt-nav]') ? active.getAttribute('aria-label') : null;
        if (label) return { selector: `a[data-dt-nav][aria-label="${CSS.escape(label)}"]` };

        // A page-number link or the error Retry button: land on the page input.
        return { selector: '[data-dt-page-input]' };
    }

    restoreFocus(focus) {
        if (!focus) return;
        const target = this.body.querySelector(focus.selector);
        const usable = target && !target.closest('.disabled') ? target : this.body.querySelector('[data-dt-page-input]');
        usable?.focus({ preventScroll: true });
    }

    announce(message) {
        if (!this.status) return;
        // Clear first so an identical message is announced again.
        this.status.textContent = '';
        setTimeout(() => { this.status.textContent = message; }, 50);
    }

    async load() {
        this.controller?.abort();
        const controller = new AbortController();
        this.controller = controller;
        this.setBusy(true);

        const url = this.url();
        const focus = this.captureFocus();
        try {
            const html = await fetchHtml(url, {
                headers: { 'X-DataTable': this.id },
                signal: controller.signal,
            });
            this.body.innerHTML = html;
            this.applyHiddenColumns();
            this.restoreFocus(focus);

            const summary = this.body.querySelector('[data-dt-summary]')?.textContent.replace(/\s+/g, ' ').trim();
            this.announce(summary ? `Showing ${summary}` : 'Could not load the data.');

            if (this.syncUrl) {
                window.history.replaceState(window.history.state, '', url);
            }
        } catch (error) {
            if (error.name === 'AbortError') return;
            this.renderError(error.message || 'Could not load the data.');
            if (focus) this.body.querySelector('[data-dt-refresh]')?.focus({ preventScroll: true });
            this.announce('Could not load the data.');
        } finally {
            if (this.controller === controller) this.setBusy(false);
        }
    }

    renderError(message) {
        const alert = document.createElement('div');
        alert.className = 'alert alert-danger d-flex flex-column flex-sm-row align-items-sm-center justify-content-between gap-2 mb-0';
        alert.setAttribute('role', 'alert');

        const text = document.createElement('div');
        const title = document.createElement('div');
        title.className = 'fw-semibold';
        title.textContent = 'Could not load the data.';
        const detail = document.createElement('div');
        detail.className = 'small';
        detail.textContent = message;
        text.append(title, detail);

        const retry = document.createElement('button');
        retry.type = 'button';
        retry.className = 'btn btn-sm btn-outline-danger flex-shrink-0';
        retry.dataset.dtRefresh = '';
        retry.textContent = 'Retry';

        alert.append(text, retry);
        this.body.replaceChildren(alert);
    }

    applyHiddenColumns() {
        this.body.querySelectorAll('[data-col]').forEach((cell) => {
            cell.classList.toggle('d-none', this.hiddenColumns.has(cell.dataset.col));
        });
    }

    exportCsv() {
        const headers = [...this.body.querySelectorAll('thead th')]
            .filter((th) => th.dataset.dtExport !== 'false' && !th.classList.contains('d-none'));
        const keys = headers.map((th) => th.dataset.col);

        const lines = [headers.map((th) => csvCell(th.textContent))];
        this.body.querySelectorAll('tbody tr[data-dt-row]').forEach((tr) => {
            lines.push(keys.map((key) => csvCell(tr.querySelector(`td[data-col="${CSS.escape(key)}"]`)?.innerText)));
        });

        // BOM so spreadsheet apps detect UTF-8.
        const blob = new Blob(['﻿', lines.map((line) => line.join(',')).join('\r\n')], { type: 'text/csv;charset=utf-8' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = `${this.root.dataset.dtExportName || this.id}.csv`;
        document.body.append(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(link.href), 0);
    }
}

function csvCell(value) {
    let text = (value ?? '').replace(/\s+/g, ' ').trim();
    // Neutralise spreadsheet formula injection.
    if (/^[=+\-@]/.test(text)) text = `'${text}`;
    return /[",\r\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

export function initDataTables(root = document) {
    root.querySelectorAll('[data-dt]').forEach((element) => {
        if (!element.dataTable) element.dataTable = new DataTable(element);
    });
}
