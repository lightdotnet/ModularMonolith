// Fetch helpers: antiforgery header on mutations, X-Requested-With so the server answers an
// expired session with 401 (not a login redirect), and a redirect to the login page on 401.

const ANTIFORGERY_HEADER = 'RequestVerificationToken';

export function readMeta(name) {
    return document.querySelector(`meta[name="${name}"]`)?.getAttribute('content') ?? '';
}

export function antiforgeryToken() {
    return readMeta('request-verification-token');
}

/** Error thrown by fetchJson/fetchHtml for a non-2xx response. */
export class HttpError extends Error {
    constructor(status, message, body) {
        super(message);
        this.name = 'HttpError';
        this.status = status;
        this.body = body;
    }
}

export function redirectToLogin() {
    const loginUrl = readMeta('login-url') || '/Account/Login';
    const returnUrl = `${window.location.pathname}${window.location.search}`;
    window.location.assign(`${loginUrl}?returnUrl=${encodeURIComponent(returnUrl)}`);
}

async function readErrorMessage(response) {
    const fallback = `Request failed with status ${response.status}.`;
    try {
        const body = await response.clone().json();
        if (typeof body?.message === 'string' && body.message) return { message: body.message, body };
        if (typeof body?.title === 'string' && body.title) return { message: body.title, body };
        return { message: fallback, body };
    } catch {
        return { message: fallback, body: null };
    }
}

async function send(url, options, accept) {
    const method = (options.method ?? 'GET').toUpperCase();
    const headers = new Headers(options.headers);

    headers.set('X-Requested-With', 'XMLHttpRequest');
    headers.set('Accept', accept);

    if (method !== 'GET' && method !== 'HEAD') {
        headers.set(ANTIFORGERY_HEADER, antiforgeryToken());
    }

    let body = options.body;
    const isRawBody = body === undefined
        || body === null
        || typeof body === 'string'
        || body instanceof FormData
        || body instanceof URLSearchParams
        || body instanceof Blob;

    if (!isRawBody) {
        headers.set('Content-Type', 'application/json');
        body = JSON.stringify(body);
    }

    const response = await fetch(url, {
        ...options,
        method,
        headers,
        body,
        credentials: 'same-origin',
    });

    if (response.status === 401) {
        redirectToLogin();
        throw new HttpError(401, 'Your session has expired. Please sign in again.', null);
    }

    if (!response.ok) {
        const { message, body: errorBody } = await readErrorMessage(response);
        throw new HttpError(response.status, message, errorBody);
    }

    return response;
}

/** Sends a request (JSON body for plain objects) with the antiforgery header; resolves to parsed JSON, or null for 204. */
export async function fetchJson(url, options = {}) {
    const response = await send(url, options, 'application/json');
    if (response.status === 204) return null;
    const text = await response.text();
    return text ? JSON.parse(text) : null;
}

/** Same as fetchJson but resolves to an HTML fragment string (e.g. a partial view to swap in). */
export async function fetchHtml(url, options = {}) {
    const response = await send(url, options, 'text/html');
    return response.text();
}
