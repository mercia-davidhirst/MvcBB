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
