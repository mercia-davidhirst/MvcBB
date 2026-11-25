// Discussion page reading aids (Threads/Details):
//  - opens at the member's first unread post, if their profile asks for it,
//    and wires up the "Jump to first unread" button either way
//  - saves how far they've read as posts scroll into view, so the next
//    visit knows which posts are new
// Guests get neither; the page renders without the data attributes' values.
(function () {
    var container = document.getElementById('threadPosts');
    if (!container) {
        return;
    }

    var posts = Array.prototype.slice.call(container.querySelectorAll('.thread-post'));
    if (posts.length === 0) {
        return;
    }

    // ---- Jump to first unread ----

    var firstUnreadId = container.getAttribute('data-first-unread');
    var firstUnread = firstUnreadId ? document.getElementById(firstUnreadId) : null;

    // Brief highlight so the eye lands on the right post after the jump.
    function flash(post) {
        post.classList.remove('post-flash');
        void post.offsetWidth; // restart the animation if it's already run
        post.classList.add('post-flash');
    }

    // 'instant' rather than 'auto' for the automatic jump: Bootstrap sets
    // scroll-behavior: smooth on the page, and a smooth scroll started while
    // the page is still loading gets cancelled and leaves it at the top.
    function jumpTo(post, smooth) {
        post.scrollIntoView({ behavior: smooth ? 'smooth' : 'instant', block: 'start' });
        flash(post);
    }

    var jumpButton = document.getElementById('jumpToUnreadBtn');
    if (jumpButton && firstUnread) {
        jumpButton.addEventListener('click', function (event) {
            event.preventDefault();
            jumpTo(firstUnread, true);
        });
    }

    // Reloads and Back/Forward restore the reader's own scroll position;
    // only a fresh visit jumps.
    function isFreshVisit() {
        try {
            var entry = performance.getEntriesByType('navigation')[0];
            if (entry) {
                return entry.type === 'navigate';
            }
        } catch (e) {
            // Navigation Timing unavailable - treat as a fresh visit.
        }
        return true;
    }

    // A link to a particular post (#post-123, e.g. from a quote or a
    // notification) wins over the automatic jump.
    if (firstUnread && container.getAttribute('data-auto-jump') === 'true' && !location.hash && isFreshVisit()) {
        var readerMoved = false;
        var markMoved = function () { readerMoved = true; };
        ['wheel', 'touchstart', 'keydown', 'mousedown'].forEach(function (type) {
            window.addEventListener(type, markMoved, { once: true, passive: true });
        });

        jumpTo(firstUnread, false);

        // Images above the post can finish loading after this and push it
        // down the page - line it up again once they have, unless the reader
        // has already scrolled somewhere else.
        window.addEventListener('load', function () {
            if (!readerMoved) {
                firstUnread.scrollIntoView({ behavior: 'instant', block: 'start' });
            }
        });
    }

    // ---- Saving the read position ----

    if (container.getAttribute('data-track-reads') !== 'true' || !('IntersectionObserver' in window)) {
        return;
    }

    var url = container.getAttribute('data-mark-read-url');
    var tokenInput = document.querySelector('#markReadForm input[name="__RequestVerificationToken"]');
    if (!url || !tokenInput) {
        return;
    }

    var seenIndex = -1;  // furthest post (in page order) seen on this visit
    var savedIndex = -1; // furthest post already sent to the server
    var saveTimer = null;
    var stopped = false;

    function save(leavingPage) {
        if (saveTimer) {
            clearTimeout(saveTimer);
            saveTimer = null;
        }
        if (stopped || seenIndex <= savedIndex) {
            return;
        }

        var index = seenIndex;
        var previous = savedIndex;
        savedIndex = index;

        var body = new FormData();
        body.append('postId', posts[index].getAttribute('data-post-id'));
        body.append('__RequestVerificationToken', tokenInput.value);

        // keepalive lets the last save finish while the page is closing.
        fetch(url, { method: 'POST', body: body, credentials: 'same-origin', keepalive: !!leavingPage })
            .then(function (response) {
                if (response.status === 401 || response.status === 400) {
                    stopped = true; // signed out or token expired - nothing more will succeed
                } else if (!response.ok && savedIndex === index) {
                    savedIndex = previous; // try again with the next post seen
                }
            })
            .catch(function () {
                if (savedIndex === index) {
                    savedIndex = previous;
                }
            });
    }

    // A post counts as read once at least half of it is on screen, or, for a
    // post taller than that, once it fills half the screen.
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) {
                return;
            }
            var readEnough = entry.intersectionRatio >= 0.5 ||
                entry.intersectionRect.height >= window.innerHeight * 0.5;
            if (!readEnough) {
                return;
            }

            observer.unobserve(entry.target);
            var index = posts.indexOf(entry.target);
            if (index > seenIndex) {
                seenIndex = index;
                // Batch a burst of scrolling into one save.
                if (!saveTimer) {
                    saveTimer = setTimeout(save, 1500);
                }
            }
        });
    }, { threshold: [0, 0.25, 0.5, 0.75, 1] });

    posts.forEach(function (post) {
        observer.observe(post);
    });

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'hidden') {
            save(true);
        }
    });
    window.addEventListener('pagehide', function () {
        save(true);
    });
})();
