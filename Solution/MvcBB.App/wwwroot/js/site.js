// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Dark/light theme toggle. The actual theme is applied by an inline script in
// _Layout's <head> (so it runs before first paint) - this just wires up the
// navbar button to flip it and remember the choice for next time.
(function () {
    function setStoredTheme(theme) {
        try {
            localStorage.setItem('mvcbb-theme', theme);
        } catch (e) {
            // Private browsing / blocked storage - the toggle still works for this page view.
        }
    }

    function applyIcon(theme) {
        var icon = document.getElementById('themeToggleIcon');
        if (icon) {
            icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        applyIcon(document.documentElement.getAttribute('data-theme') || 'light');

        var btn = document.getElementById('themeToggleBtn');
        if (btn) {
            btn.addEventListener('click', function () {
                var next = document.documentElement.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
                document.documentElement.setAttribute('data-theme', next);
                setStoredTheme(next);
                applyIcon(next);
            });
        }
    });
})();

// On-screen alert for new private messages. Polls a lightweight JSON
// endpoint rather than pushing over a socket - this app has no real-time
// transport (no SignalR/WebSockets), so a periodic check is the simplest
// way to surface a new PM without the user navigating anywhere.
(function () {
    if (typeof window.mvcbbInitialUnreadCount === 'undefined') {
        return; // Not authenticated - _Layout never set this.
    }

    var POLL_INTERVAL_MS = 30000;
    var lastKnownCount = window.mvcbbInitialUnreadCount;

    function updateBadge(count) {
        var badge = document.getElementById('unreadMessageBadge');
        if (!badge) {
            return;
        }
        badge.textContent = count;
        badge.classList.toggle('d-none', count === 0);
    }

    function showToast(count) {
        var toastEl = document.getElementById('newMessageToast');
        var textEl = document.getElementById('newMessageToastText');
        if (!toastEl || !window.bootstrap) {
            return;
        }
        if (textEl) {
            textEl.textContent = count === 1
                ? 'You have a new private message.'
                : 'You have ' + count + ' new private messages.';
        }
        var toast = window.bootstrap.Toast.getOrCreateInstance(toastEl);
        toast.show();
    }

    function poll() {
        fetch('/Messages/UnreadCount', { headers: { 'Accept': 'application/json' } })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (data) {
                if (!data) {
                    return;
                }
                updateBadge(data.count);
                if (data.count > lastKnownCount) {
                    showToast(data.count);
                }
                lastKnownCount = data.count;
            })
            .catch(function () {
                // Transient network/API hiccup - just skip this tick, next poll retries.
            });
    }

    setInterval(poll, POLL_INTERVAL_MS);
})();

// Open-reports alert for moderators and administrators. Same approach as the
// new-PM alert above: _Layout renders the current count into the navbar
// badge and sets a baseline, then this polls a small JSON endpoint and
// updates the badge, toasting when the count goes up (a new report came in).
(function () {
    if (typeof window.mvcbbInitialOpenReportCount === 'undefined') {
        return; // Not a moderator/administrator - _Layout never set this.
    }

    var POLL_INTERVAL_MS = 30000;
    var lastKnownCount = window.mvcbbInitialOpenReportCount;

    function updateBadge(count) {
        var badge = document.getElementById('openReportBadge');
        if (!badge) {
            return;
        }
        badge.textContent = count;
        badge.classList.toggle('d-none', count === 0);
    }

    function showToast(count) {
        var toastEl = document.getElementById('newReportToast');
        var textEl = document.getElementById('newReportToastText');
        if (!toastEl || !window.bootstrap) {
            return;
        }
        if (textEl) {
            textEl.textContent = count === 1
                ? 'There is 1 open report to review.'
                : 'There are ' + count + ' open reports to review.';
        }
        window.bootstrap.Toast.getOrCreateInstance(toastEl).show();
    }

    function poll() {
        fetch('/Reports/OpenCount', { headers: { 'Accept': 'application/json' } })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (data) {
                if (!data) {
                    return;
                }
                updateBadge(data.count);
                if (data.count > lastKnownCount) {
                    showToast(data.count);
                }
                lastKnownCount = data.count;
            })
            .catch(function () {
                // Transient network/API hiccup - just skip this tick, next poll retries.
            });
    }

    setInterval(poll, POLL_INTERVAL_MS);
})();

// Bootstrap tooltips for any element marked data-bs-toggle="tooltip" (e.g.
// the navbar's "Report an issue" and "Open reports" icons). The title
// attribute still works as a plain tooltip if Bootstrap's JS isn't loaded.
document.addEventListener('DOMContentLoaded', function () {
    if (!window.bootstrap || !window.bootstrap.Tooltip) {
        return;
    }
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (el) {
        window.bootstrap.Tooltip.getOrCreateInstance(el);
    });
});

// Board listing: category collapse state, persisted client-side the same way
// as the dark/light theme toggle above - a JSON map of categoryId -> collapsed,
// applied directly (no animation) on load, then kept in sync with Bootstrap's
// own collapse events as the user toggles a category.
(function () {
    var STORAGE_KEY = 'mvcbb-category-collapsed';

    function getCollapsedMap() {
        try {
            return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}');
        } catch (e) {
            return {};
        }
    }

    function setCollapsed(categoryId, collapsed) {
        try {
            var map = getCollapsedMap();
            if (collapsed) {
                map[categoryId] = true;
            } else {
                delete map[categoryId];
            }
            localStorage.setItem(STORAGE_KEY, JSON.stringify(map));
        } catch (e) {
            // Private browsing / blocked storage - the toggle still works for this page view.
        }
    }

    function setChevron(toggleBtn, collapsed) {
        var icon = toggleBtn.querySelector('i');
        if (icon) {
            icon.className = collapsed ? 'bi bi-chevron-right' : 'bi bi-chevron-down';
        }
        toggleBtn.setAttribute('aria-expanded', (!collapsed).toString());
    }

    document.addEventListener('DOMContentLoaded', function () {
        var map = getCollapsedMap();
        document.querySelectorAll('.category-collapse-toggle').forEach(function (toggleBtn) {
            var categoryId = toggleBtn.getAttribute('data-category-id');
            var body = document.getElementById('category-collapse-' + categoryId);
            if (!body || !map[categoryId]) {
                return;
            }
            body.classList.remove('show');
            setChevron(toggleBtn, true);
        });

        document.querySelectorAll('.category-collapse-body').forEach(function (body) {
            body.addEventListener('shown.bs.collapse', function (event) {
                if (event.target !== body) return;
                var categoryId = body.id.replace('category-collapse-', '');
                setCollapsed(categoryId, false);
                var toggleBtn = document.querySelector('.category-collapse-toggle[data-category-id="' + categoryId + '"]');
                if (toggleBtn) setChevron(toggleBtn, false);
            });
            body.addEventListener('hidden.bs.collapse', function (event) {
                if (event.target !== body) return;
                var categoryId = body.id.replace('category-collapse-', '');
                setCollapsed(categoryId, true);
                var toggleBtn = document.querySelector('.category-collapse-toggle[data-category-id="' + categoryId + '"]');
                if (toggleBtn) setChevron(toggleBtn, true);
            });
        });
    });
})();

// Board listing: each board's "recent threads" are loaded on demand instead
// of eagerly for every board on initial page render (that was causing the
// page to time out as the board count grew). First click fetches and caches
// the markup; later clicks just toggle visibility.
(function () {
    document.addEventListener('click', function (event) {
        var btn = event.target.closest('.board-recent-threads-toggle');
        if (!btn) {
            return;
        }

        var boardId = btn.getAttribute('data-board-id');
        var container = document.getElementById('recent-threads-' + boardId);
        if (!container) {
            return;
        }

        if (container.dataset.loaded === 'true') {
            var nowHidden = container.classList.toggle('d-none');
            btn.innerHTML = nowHidden
                ? '<i class="bi bi-chat-left-text"></i> Show recent threads'
                : '<i class="bi bi-chat-left-text"></i> Hide recent threads';
            return;
        }

        btn.disabled = true;
        fetch('/Boards/GetRecentThreads?boardId=' + encodeURIComponent(boardId))
            .then(function (response) { return response.ok ? response.text() : null; })
            .then(function (html) {
                if (html !== null) {
                    container.innerHTML = html;
                    container.dataset.loaded = 'true';
                    container.classList.remove('d-none');
                    btn.innerHTML = '<i class="bi bi-chat-left-text"></i> Hide recent threads';
                }
            })
            .catch(function () {
                // Transient network/API hiccup - leave the button enabled so the user can retry.
            })
            .finally(function () {
                btn.disabled = false;
            });
    });
})();

// Password reveal toggle. A single delegated listener handles every
// .password-toggle-btn on the page, so each password field's markup just
// needs the button - no per-page wiring.
(function () {
    document.addEventListener('click', function (event) {
        var btn = event.target.closest('.password-toggle-btn');
        if (!btn) {
            return;
        }

        var input = btn.closest('.input-group')?.querySelector('input');
        var icon = btn.querySelector('i');
        if (!input || !icon) {
            return;
        }

        var revealing = input.type === 'password';
        input.type = revealing ? 'text' : 'password';
        icon.classList.toggle('bi-eye', !revealing);
        icon.classList.toggle('bi-eye-slash', revealing);
        btn.setAttribute('aria-label', revealing ? 'Hide password' : 'Show password');
    });
})();

// Floating "Back to top" button (in _Layout). Hidden near the top of the
// page and fades in once the reader has scrolled more than a screen down,
// which on a long discussion means it's there when they reach the end.
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var btn = document.getElementById('backToTopBtn');
        if (!btn) {
            return;
        }

        btn.hidden = false;
        var ticking = false;

        function update() {
            ticking = false;
            btn.classList.toggle('show', window.scrollY > window.innerHeight);
        }

        window.addEventListener('scroll', function () {
            if (!ticking) {
                ticking = true;
                window.requestAnimationFrame(update);
            }
        }, { passive: true });
        update();

        btn.addEventListener('click', function () {
            var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            window.scrollTo({ top: 0, behavior: reduceMotion ? 'auto' : 'smooth' });
            btn.blur();
        });
    });
})();
