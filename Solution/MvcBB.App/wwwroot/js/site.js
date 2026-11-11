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
