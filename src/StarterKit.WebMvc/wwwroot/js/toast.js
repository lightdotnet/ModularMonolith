// Toasts: a replaceable handler (default: Bootstrap toasts in #toast-container) plus the
// TempData flash messages the Toasts view component hands over as JSON.

const TOAST_VARIANTS = new Set(['primary', 'secondary', 'success', 'danger', 'warning', 'info', 'dark']);

function defaultToast(message, variant) {
    const container = document.getElementById('toast-container');
    if (!container || !window.bootstrap) return;

    const toast = document.createElement('div');
    toast.className = `toast align-items-center border-0 text-bg-${TOAST_VARIANTS.has(variant) ? variant : 'info'}`;
    toast.setAttribute('role', variant === 'danger' ? 'alert' : 'status');

    const row = document.createElement('div');
    row.className = 'd-flex';

    const text = document.createElement('div');
    text.className = 'toast-body';
    text.textContent = message; // never interpret the message as HTML

    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'btn-close btn-close-white me-2 m-auto';
    close.setAttribute('data-bs-dismiss', 'toast');
    close.setAttribute('aria-label', 'Close');

    row.append(text, close);
    toast.append(row);
    container.append(toast);

    toast.addEventListener('hidden.bs.toast', () => toast.remove());
    new window.bootstrap.Toast(toast).show();
}

let toastHandler = defaultToast;

/** Replaces the toast renderer (hook for a richer toast component). */
export function setToastHandler(handler) {
    toastHandler = typeof handler === 'function' ? handler : defaultToast;
}

/** Shows a toast; variant is a Bootstrap contextual name (success, danger, warning, info, ...). */
export function showToast(message, variant = 'info') {
    if (message) toastHandler(String(message), variant);
}

/** Shows the flash messages rendered into #flash-messages (post/redirect/get results). */
export function showFlashMessages() {
    const data = document.getElementById('flash-messages');
    if (!data) return;

    try {
        const messages = JSON.parse(data.textContent || '[]');
        for (const { type, message } of messages) showToast(message, type);
    } catch {
        // Malformed data block — nothing to show.
    }
    data.remove();
}
