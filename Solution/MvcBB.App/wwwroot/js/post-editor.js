// Post editor helpers, loaded on every page by _Layout. They only switch on
// for a textarea that asks for them:
//
//   data-draft-key="reply-12"   autosave what's typed to localStorage (see
//   data-draft-user="5"         MvcBB.App.Models.DraftKeys), and offer it back
//                               if the page is opened again before the post
//                               was saved. Inputs in the same form marked
//                               data-draft-field="name" are saved with it.
//   data-mentions="true"        suggest member names after typing "@".
//
// Drafts never leave the browser. The server tells the page which draft to
// delete once a post has really been saved (<meta name="mvcbb-clear-draft">,
// rendered by _Layout from TempData), so a post lost to an expired session
// or an error can still be restored.
(function () {
    'use strict';

    var PREFIX = 'mvcbb-draft:';
    var MAX_AGE_MS = 14 * 24 * 60 * 60 * 1000;
    var SAVE_DELAY_MS = 800;

    function storage() {
        try {
            var s = window.localStorage;
            var probe = PREFIX + 'probe';
            s.setItem(probe, '1');
            s.removeItem(probe);
            return s;
        } catch (e) {
            return null; // Private browsing / blocked storage: no drafts, editor still works.
        }
    }

    function storageKey(user, key) {
        return PREFIX + (user || 'guest') + ':' + key;
    }

    function readDraft(store, fullKey) {
        try {
            var draft = JSON.parse(store.getItem(fullKey) || 'null');
            return draft && typeof draft.content === 'string' ? draft : null;
        } catch (e) {
            return null;
        }
    }

    // Drops drafts older than MAX_AGE_MS, so localStorage doesn't fill up with
    // replies someone started weeks ago and never came back to.
    function pruneOldDrafts(store) {
        var now = Date.now();
        var stale = [];
        for (var i = 0; i < store.length; i++) {
            var k = store.key(i);
            if (k && k.indexOf(PREFIX) === 0) {
                var d = readDraft(store, k);
                if (!d || !d.savedAt || now - d.savedAt > MAX_AGE_MS) {
                    stale.push(k);
                }
            }
        }
        stale.forEach(function (k) { store.removeItem(k); });
    }

    function clearSavedDrafts(store) {
        document.querySelectorAll('meta[name="mvcbb-clear-draft"]').forEach(function (meta) {
            var user = meta.getAttribute('data-user');
            var key = meta.getAttribute('content');
            if (key) {
                store.removeItem(storageKey(user, key));
            }
        });
    }

    function timeText(ms) {
        try {
            return new Date(ms).toLocaleString([], { dateStyle: 'medium', timeStyle: 'short' });
        } catch (e) {
            return new Date(ms).toLocaleString();
        }
    }

    function setupDraft(store, textarea) {
        var fullKey = storageKey(textarea.getAttribute('data-draft-user'), textarea.getAttribute('data-draft-key'));
        var form = textarea.form;
        var fields = form ? Array.prototype.slice.call(form.querySelectorAll('[data-draft-field]')) : [];
        var initialContent = textarea.value;

        var status = document.createElement('div');
        status.className = 'draft-status form-text text-muted';
        status.setAttribute('aria-live', 'polite');
        textarea.insertAdjacentElement('afterend', status);

        var timer = null;

        function snapshot() {
            var values = {};
            fields.forEach(function (f) { values[f.getAttribute('data-draft-field')] = f.value; });
            return { content: textarea.value, fields: values, savedAt: Date.now() };
        }

        function save() {
            timer = null;
            var data = snapshot();
            var hasFieldText = Object.keys(data.fields).some(function (k) { return (data.fields[k] || '').trim() !== ''; });
            if (data.content === initialContent && !hasFieldText) {
                store.removeItem(fullKey); // Back to how the page started: nothing worth keeping.
                status.textContent = '';
                return;
            }
            try {
                store.setItem(fullKey, JSON.stringify(data));
                status.innerHTML = '<i class="bi bi-cloud-check"></i> Draft saved on this device at ' +
                    new Date(data.savedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            } catch (e) {
                status.textContent = 'Couldn\'t save a draft (browser storage is full).';
            }
        }

        function scheduleSave() {
            if (timer) {
                clearTimeout(timer);
            }
            timer = setTimeout(save, SAVE_DELAY_MS);
        }

        textarea.addEventListener('input', scheduleSave);
        fields.forEach(function (f) { f.addEventListener('input', scheduleSave); });
        // Catch the last few keystrokes if the page is closed mid-typing.
        window.addEventListener('pagehide', function () {
            if (timer) {
                clearTimeout(timer);
                save();
            }
        });

        var existing = readDraft(store, fullKey);
        if (!existing || existing.content === textarea.value) {
            return;
        }

        var banner = document.createElement('div');
        banner.className = 'alert alert-info d-flex flex-wrap align-items-center gap-2 py-2 draft-restore';
        banner.setAttribute('role', 'status');
        var text = document.createElement('span');
        text.className = 'me-auto';
        text.innerHTML = '<i class="bi bi-clock-history me-1"></i>';
        text.appendChild(document.createTextNode('You have an unsent draft from ' + timeText(existing.savedAt) + '.'));
        var restoreBtn = document.createElement('button');
        restoreBtn.type = 'button';
        restoreBtn.className = 'btn btn-sm btn-primary';
        restoreBtn.textContent = 'Restore draft';
        var discardBtn = document.createElement('button');
        discardBtn.type = 'button';
        discardBtn.className = 'btn btn-sm btn-outline-secondary';
        discardBtn.textContent = 'Discard';
        banner.appendChild(text);
        banner.appendChild(restoreBtn);
        banner.appendChild(discardBtn);

        var anchor = textarea.closest('.mb-3, .form-group') || textarea;
        anchor.parentNode.insertBefore(banner, anchor);

        restoreBtn.addEventListener('click', function () {
            textarea.value = existing.content;
            fields.forEach(function (f) {
                var name = f.getAttribute('data-draft-field');
                if (existing.fields && typeof existing.fields[name] === 'string') {
                    f.value = existing.fields[name];
                }
            });
            banner.remove();
            textarea.focus();
            // Refresh the preview and re-save under the current time.
            textarea.dispatchEvent(new Event('input', { bubbles: true }));
        });

        discardBtn.addEventListener('click', function () {
            store.removeItem(fullKey);
            banner.remove();
        });
    }

    // ---- @mention suggestions -------------------------------------------

    function setupMentions(textarea) {
        var list = document.createElement('div');
        list.className = 'list-group shadow mention-suggestions d-none';
        list.setAttribute('role', 'listbox');
        document.body.appendChild(list);

        var items = [];
        var active = -1;
        var range = null; // { start, end } of the "@partial" being completed
        var requestId = 0;
        var debounce = null;

        function hide() {
            list.classList.add('d-none');
            items = [];
            active = -1;
            range = null;
        }

        function position() {
            var rect = textarea.getBoundingClientRect();
            list.style.left = (rect.left + window.scrollX) + 'px';
            list.style.top = (rect.bottom + window.scrollY + 2) + 'px';
        }

        function highlight(index) {
            active = index;
            Array.prototype.forEach.call(list.children, function (el, i) {
                el.classList.toggle('active', i === index);
                el.setAttribute('aria-selected', i === index ? 'true' : 'false');
            });
        }

        // "@Name" when the name is plain, "@"Some Name"" otherwise - the same
        // rule as MvcBB.Shared.Services.MentionParser.Format.
        function formatMention(name) {
            return /^[\p{L}\p{N}_][\p{L}\p{N}_.\-]*[\p{L}\p{N}_]$/u.test(name) ? '@' + name : '@"' + name + '"';
        }

        function choose(index) {
            if (!range || index < 0 || index >= items.length) {
                return;
            }
            var insert = formatMention(items[index]) + ' ';
            var value = textarea.value;
            textarea.value = value.substring(0, range.start) + insert + value.substring(range.end);
            var caret = range.start + insert.length;
            textarea.setSelectionRange(caret, caret);
            hide();
            textarea.focus();
            textarea.dispatchEvent(new Event('input', { bubbles: true }));
        }

        function show(names) {
            items = names;
            list.innerHTML = '';
            if (names.length === 0) {
                hide();
                return;
            }
            names.forEach(function (name, i) {
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'list-group-item list-group-item-action py-1';
                btn.setAttribute('role', 'option');
                btn.textContent = '@' + name;
                btn.addEventListener('mousedown', function (e) {
                    e.preventDefault(); // Keep focus (and the caret) in the textarea.
                    choose(i);
                });
                list.appendChild(btn);
            });
            position();
            list.classList.remove('d-none');
            highlight(0);
        }

        // The "@partial" right before the caret, if the member is typing one.
        function currentMention() {
            var caret = textarea.selectionStart;
            if (caret !== textarea.selectionEnd) {
                return null;
            }
            var before = textarea.value.substring(Math.max(0, caret - 52), caret);
            var m = /(^|[^\w@])@("?)([^\s@"]{1,50}|[^@"\r\n]{1,50})$/u.exec(before);
            if (!m) {
                return null;
            }
            var query = m[3];
            if (!m[2] && /\s/.test(query)) {
                return null;
            }
            return { query: query, start: caret - query.length - m[2].length - 1, end: caret };
        }

        function lookup() {
            var mention = currentMention();
            if (!mention || mention.query.length < 2) {
                hide();
                return;
            }
            var id = ++requestId;
            fetch('/Members/MentionSuggestions?q=' + encodeURIComponent(mention.query), { headers: { 'Accept': 'application/json' } })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (names) {
                    if (id !== requestId) {
                        return; // A newer keystroke has already asked again.
                    }
                    range = { start: mention.start, end: mention.end };
                    show(Array.isArray(names) ? names : []);
                })
                .catch(hide);
        }

        textarea.addEventListener('input', function () {
            if (debounce) {
                clearTimeout(debounce);
            }
            debounce = setTimeout(lookup, 200);
        });

        textarea.addEventListener('keydown', function (e) {
            if (list.classList.contains('d-none')) {
                return;
            }
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                highlight((active + 1) % items.length);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                highlight((active - 1 + items.length) % items.length);
            } else if (e.key === 'Enter' || e.key === 'Tab') {
                e.preventDefault();
                choose(active);
            } else if (e.key === 'Escape') {
                hide();
            }
        });

        textarea.addEventListener('blur', function () { setTimeout(hide, 150); });
        window.addEventListener('resize', function () {
            if (!list.classList.contains('d-none')) {
                position();
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        var store = storage();
        if (store) {
            clearSavedDrafts(store);
            pruneOldDrafts(store);
            document.querySelectorAll('textarea[data-draft-key]').forEach(function (t) { setupDraft(store, t); });
        }
        document.querySelectorAll('textarea[data-mentions="true"]').forEach(setupMentions);
    });
})();
