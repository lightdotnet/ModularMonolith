// <local-datetime>: rewrites <time data-local-datetime="datetime|date|time|relative" datetime="ISO">
// in the browser's time zone and locale. Also formats elements added later (data-table refreshes,
// the bell dropdown) through a MutationObserver.

const FORMATS = {
    datetime: { dateStyle: 'medium', timeStyle: 'short' },
    date: { dateStyle: 'medium' },
    time: { timeStyle: 'short' },
};

const RELATIVE_UNITS = [
    ['year', 365 * 24 * 3600],
    ['month', 30 * 24 * 3600],
    ['week', 7 * 24 * 3600],
    ['day', 24 * 3600],
    ['hour', 3600],
    ['minute', 60],
];

function formatRelative(date) {
    const seconds = Math.round((date.getTime() - Date.now()) / 1000);
    const formatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' });

    for (const [unit, size] of RELATIVE_UNITS) {
        if (Math.abs(seconds) >= size) return formatter.format(Math.round(seconds / size), unit);
    }
    return formatter.format(0, 'minute');
}

function formatElement(element) {
    const date = new Date(element.getAttribute('datetime'));
    if (Number.isNaN(date.getTime())) return;

    const kind = element.dataset.localDatetime || 'datetime';
    element.textContent = kind === 'relative'
        ? formatRelative(date)
        : new Intl.DateTimeFormat(undefined, FORMATS[kind] ?? FORMATS.datetime).format(date);
    element.title = new Intl.DateTimeFormat(undefined, FORMATS.datetime).format(date);
    element.dataset.localFormatted = 'true';
}

export function formatLocalDateTimes(root = document) {
    root.querySelectorAll('time[data-local-datetime]:not([data-local-formatted])').forEach(formatElement);
}

export function initLocalDateTimes() {
    formatLocalDateTimes();

    new MutationObserver((mutations) => {
        for (const mutation of mutations) {
            for (const node of mutation.addedNodes) {
                if (node instanceof Element) {
                    if (node.matches('time[data-local-datetime]')) formatElement(node);
                    formatLocalDateTimes(node);
                }
            }
        }
    }).observe(document.body, { childList: true, subtree: true });
}
