// Navbar bell + real-time notifications, mirroring the admin client's use-notifications.ts:
// - the browser connects directly to the backend SignalR hub with a short-lived hub token minted
//   by GET /notifications/hub-token; the first negotiate reuses the token minted to discover the
//   hub URL, every later (re)connect mints a fresh one;
// - the hub pushes a "SystemMessage" (raw payload: title/message/url — no id/status), so the badge
//   and lists are re-fetched rather than merged; they are also re-fetched after a reconnect, since
//   pushes may have been missed while offline;
// - once automatic reconnect gives up (or the first start fails), a restart is retried every
//   RETRY_AFTER_FAILURE_MS, forever;
// - a "ForceLogoutMessage" for this user signs the browser session out.
// Requires the @microsoft/signalr browser bundle (window.signalR), loaded (deferred) before this
// module at the end of the layout.

import { fetchJson, fetchHtml } from './http.js';
import { showToast } from './toast.js';

const RETRY_AFTER_FAILURE_MS = 30_000;

// Data tables that list notifications and should reload on a push (inbox + admin list).
const NOTIFICATION_TABLE_IDS = ['inbox', 'notifications'];

const numberFormat = new Intl.NumberFormat('en-US');

const bell = document.querySelector('[data-notification-bell]');

function setUnreadCount(count) {
    const badge = bell.querySelector('[data-bell-badge]');
    const icon = bell.querySelector('[data-bell-icon]');
    const button = bell.querySelector('[data-bell-button]');
    const value = Number(count) || 0;

    badge.textContent = value > 99 ? '99+' : String(value);
    badge.classList.toggle('d-none', value === 0);
    icon?.classList.toggle('bell-ring', value > 0);
    button?.setAttribute('aria-label', value > 0 ? `Notifications, ${numberFormat.format(value)} unread` : 'Notifications');
}

async function refreshUnreadCount() {
    try {
        const result = await fetchJson(bell.dataset.unreadCountUrl);
        setUnreadCount(result?.count);
    } catch {
        // Keep the last known count.
    }
}

async function loadItems() {
    const container = bell.querySelector('[data-bell-items]');
    try {
        container.innerHTML = await fetchHtml(bell.dataset.bellItemsUrl);
    } catch {
        const message = document.createElement('div');
        message.className = 'text-center text-body-secondary small py-4 px-3';
        message.textContent = 'Notifications could not be loaded.';
        container.replaceChildren(message);
    }
}

function isOpen() {
    return bell.querySelector('.dropdown-menu')?.classList.contains('show') ?? false;
}

async function refreshAll() {
    await refreshUnreadCount();
    if (isOpen()) await loadItems();
    // Let the inbox/admin data tables on the page reload too (other tables are left alone).
    for (const id of NOTIFICATION_TABLE_IDS) {
        document.dispatchEvent(new CustomEvent('dt:refresh', { detail: { id } }));
    }
}

function markedText(count) {
    if (count <= 0) return 'No unread notifications.';
    return `Marked ${numberFormat.format(count)} ${count === 1 ? 'notification' : 'notifications'} as read.`;
}

async function markAllRead(button) {
    button.disabled = true;
    try {
        const result = await fetchJson(bell.dataset.markAllReadUrl, { method: 'POST' });
        const marked = Number(result?.marked) || 0;
        showToast(result?.message || markedText(marked), marked > 0 ? 'success' : 'info');
        await refreshAll();
    } catch (error) {
        showToast(error.message || 'Could not mark notifications as read.', 'danger');
    } finally {
        button.disabled = false;
    }
}

async function mintHubToken() {
    try {
        return await fetchJson(bell.dataset.hubTokenUrl);
    } catch {
        return null;
    }
}

// SignalR embeds the raw negotiate response in its error; strip an HTML error page from the log.
function sanitizeError(error) {
    const message = error instanceof Error ? error.message : String(error);
    const htmlIndex = message.search(/<!DOCTYPE html|<html[\s>]/i);
    return htmlIndex === -1 ? message : message.slice(0, htmlIndex).trim();
}

function scheduleRestart(connection) {
    setTimeout(() => void startConnection(connection, true), RETRY_AFTER_FAILURE_MS);
}

async function startConnection(connection, isRestart) {
    try {
        await connection.start();
        // Anything pushed while disconnected was missed.
        if (isRestart) await refreshAll();
    } catch (error) {
        console.error(sanitizeError(error));
        scheduleRestart(connection);
    }
}

async function connect() {
    const signalR = window.signalR;
    if (!signalR) return;

    const initial = await mintHubToken();
    if (!initial?.hubUrl) {
        setTimeout(() => void connect(), RETRY_AFTER_FAILURE_MS);
        return;
    }

    // The token minted above is only used for the first negotiate; later ones mint their own.
    let pendingToken = initial.accessToken ?? null;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(initial.hubUrl, {
            accessTokenFactory: async () => {
                if (pendingToken) {
                    const token = pendingToken;
                    pendingToken = null;
                    return token;
                }
                return (await mintHubToken())?.accessToken ?? '';
            },
            transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Critical)
        .build();

    connection.on('SystemMessage', (message) => {
        if (message?.title) showToast(message.title, 'info');
        void refreshAll();
    });

    connection.on('ForceLogoutMessage', () => {
        document.getElementById('session-logout-form')?.submit();
    });

    connection.onreconnected(() => void refreshAll());

    // Fired once automatic reconnect has given up (not on a failed start()).
    connection.onclose(() => scheduleRestart(connection));

    await startConnection(connection, false);
}

if (bell) {
    bell.addEventListener('show.bs.dropdown', () => void loadItems());
    bell.querySelector('[data-bell-mark-all]')?.addEventListener('click', (event) => void markAllRead(event.currentTarget));
    void connect();
}
