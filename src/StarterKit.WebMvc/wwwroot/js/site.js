// Entry module loaded by both layouts (no jQuery). Wires the shared UI behaviours and re-exports
// the helpers so page scripts can do:
//   import { fetchJson, fetchHtml, showToast } from '/js/site.js';
// Non-module scripts can use window.app.* instead.

import { fetchJson, fetchHtml, HttpError } from './http.js';
import { showToast, setToastHandler, showFlashMessages } from './toast.js';
import { initConfirm } from './confirm.js';
import { initLocalDateTimes } from './local-datetime.js';
import { initDataTables } from './data-table.js';
import { initUserLookups } from './user-lookup.js';

export { fetchJson, fetchHtml, HttpError, showToast, setToastHandler };

function initValidation() {
    const lib = window.aspnetValidation;
    if (!lib) return null;

    const service = new lib.ValidationService();
    service.ValidationInputCssClassName = 'is-invalid';
    service.bootstrap({ watch: true });
    return service;
}

// Forms marked data-loading-form (or holding a <submit-button>) show their spinner and lock their
// fieldsets once a submit actually proceeds. The validation library stops an invalid (or still
// validating) submit before it bubbles here, so this only runs for a submit that goes through.
document.addEventListener('submit', (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || event.defaultPrevented) return;
    if (!form.hasAttribute('data-loading-form') && !form.querySelector('[data-submit-button]')) return;

    form.querySelectorAll('input[data-current-url]').forEach((input) => {
        input.value = `${window.location.pathname}${window.location.search}`;
    });
    form.querySelectorAll('[data-loading-indicator]').forEach((el) => el.classList.remove('d-none'));
    form.setAttribute('aria-busy', 'true');
    // Disable after the submit has captured the field values.
    setTimeout(() => form.querySelectorAll('fieldset').forEach((fieldset) => { fieldset.disabled = true; }), 0);
});

// A page restored from the back/forward cache must not stay locked.
window.addEventListener('pageshow', (event) => {
    if (!event.persisted) return;
    document.querySelectorAll('form[aria-busy="true"]').forEach((form) => {
        form.removeAttribute('aria-busy');
        form.querySelectorAll('fieldset').forEach((fieldset) => { fieldset.disabled = false; });
        form.querySelectorAll('[data-loading-indicator]').forEach((el) => el.classList.add('d-none'));
    });
});

const validation = initValidation();
initConfirm();
initLocalDateTimes();
initDataTables();
initUserLookups();
showFlashMessages();

window.app = Object.freeze({
    fetchJson,
    fetchHtml,
    showToast,
    setToastHandler,
    validation,
});
