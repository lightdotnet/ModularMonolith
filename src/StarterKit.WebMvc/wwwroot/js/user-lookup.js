// User look-up for plain inputs: <input data-user-lookup="/lookup/url" list="datalist-id">.
// After 3+ typed characters (debounced) it fills the <datalist> with matching users; the option
// value is the user ID (what the form posts), the label shows the name. Look-up failures (e.g. no
// permission to search users) are silent — the input still accepts a typed ID.

import { fetchJson } from './http.js';

const MIN_LENGTH = 3;
const DEBOUNCE_MS = 400;

function bind(input) {
    const list = input.list;
    if (!list || input.dataset.userLookupBound) return;
    input.dataset.userLookupBound = 'true';

    let timer = null;
    let controller = null;

    input.addEventListener('input', () => {
        clearTimeout(timer);
        const query = input.value.trim();
        if (query.length < MIN_LENGTH) return;

        timer = setTimeout(async () => {
            controller?.abort();
            controller = new AbortController();

            try {
                const url = `${input.dataset.userLookup}?q=${encodeURIComponent(query)}`;
                const users = await fetchJson(url, { signal: controller.signal });

                list.replaceChildren(...(users ?? []).map((user) => {
                    const option = document.createElement('option');
                    option.value = user.id;
                    option.label = user.displayName ? `${user.displayName} (${user.userName})` : user.userName;
                    return option;
                }));
            } catch {
                // Aborted or not permitted — keep manual entry working.
            }
        }, DEBOUNCE_MS);
    });
}

export function initUserLookups(root = document) {
    root.querySelectorAll('input[data-user-lookup]').forEach(bind);
}
