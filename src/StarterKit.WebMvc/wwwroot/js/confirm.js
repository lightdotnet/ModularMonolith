// <confirm-button> behaviour: opens the shared #confirm-modal and, on confirmation, either submits
// the referenced form (data-confirm-form) or POSTs to data-confirm-url with the antiforgery token
// and the current page URL as returnUrl (so the list comes back with the same filters/page).

import { antiforgeryToken } from './http.js';

const ANTIFORGERY_FIELD = '__RequestVerificationToken';

let pendingTrigger = null;

function modalParts(modal) {
    return {
        title: modal.querySelector('[data-confirm-modal-title]'),
        message: modal.querySelector('[data-confirm-modal-message]'),
        accept: modal.querySelector('[data-confirm-modal-accept]'),
        acceptText: modal.querySelector('[data-confirm-modal-accept-text]'),
        spinner: modal.querySelector('[data-confirm-modal-accept] [data-loading-indicator]'),
    };
}

function postTo(url) {
    const form = document.createElement('form');
    form.method = 'post';
    form.action = url;
    form.hidden = true;

    const token = document.createElement('input');
    token.type = 'hidden';
    token.name = ANTIFORGERY_FIELD;
    token.value = antiforgeryToken();

    const returnUrl = document.createElement('input');
    returnUrl.type = 'hidden';
    returnUrl.name = 'returnUrl';
    returnUrl.value = `${window.location.pathname}${window.location.search}`;

    form.append(token, returnUrl);
    document.body.append(form);
    form.submit();
}

function accept(modal) {
    const trigger = pendingTrigger;
    if (!trigger) return;

    const { accept: button, spinner } = modalParts(modal);
    button.disabled = true;
    spinner?.classList.remove('d-none');

    const formId = trigger.dataset.confirmForm;
    if (formId) {
        const form = document.getElementById(formId);
        window.bootstrap?.Modal.getInstance(modal)?.hide();
        form?.requestSubmit();
        return;
    }

    postTo(trigger.dataset.confirmUrl);
}

function open(trigger) {
    const modal = document.getElementById('confirm-modal');
    if (!modal || !window.bootstrap) return;

    const parts = modalParts(modal);
    const variant = trigger.dataset.confirmVariant || 'danger';

    parts.title.textContent = trigger.dataset.confirmTitle || 'Are you sure?';
    parts.message.textContent = trigger.dataset.confirmMessage || '';
    parts.message.classList.toggle('d-none', !trigger.dataset.confirmMessage);
    parts.acceptText.textContent = trigger.dataset.confirmText || 'Confirm';
    parts.accept.className = `btn btn-${variant} w-100 w-sm-auto`;
    parts.accept.disabled = false;
    parts.spinner?.classList.add('d-none');

    pendingTrigger = trigger;
    window.bootstrap.Modal.getOrCreateInstance(modal).show();
}

export function initConfirm() {
    document.addEventListener('click', (event) => {
        const trigger = event.target.closest('[data-confirm]');
        if (!trigger) return;
        event.preventDefault();
        open(trigger);
    });

    const modal = document.getElementById('confirm-modal');
    if (!modal) return;

    modalParts(modal).accept.addEventListener('click', () => accept(modal));
    modal.addEventListener('hidden.bs.modal', () => {
        // The modal is opened programmatically, so Bootstrap does not return focus on its own:
        // put keyboard/screen-reader users back on the button that opened it (if still on the page).
        const trigger = pendingTrigger;
        pendingTrigger = null;
        if (trigger?.isConnected) trigger.focus({ preventScroll: true });
    });
}
