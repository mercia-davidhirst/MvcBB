# MvcBB

A modern bulletin-board (forum) system built on ASP.NET Core (.NET 10),
structured as a two-tier MVC + API application with a swappable persistence
layer and a drop-in mod framework. MvcBB is white-label: site name, logo,
welcome text and footer are all set from the admin panel, so the same build
can run any community.

## Architecture

- **MvcBB.App** — the public-facing MVC web app (Razor views, controllers).
  Talks to `MvcBB.API` over HTTP/JWT; holds no direct database access itself.
  Browser sessions use a cookie; the API's JWTs are only used for the App's
  own outbound calls to the API.
- **MvcBB.API** — the backend API. Issues JWTs (per-user login tokens and a
  client-credentials token for anonymous/service calls), hosts all business
  logic, and delegates persistence to whichever repository implementation is
  configured.
- **MvcBB.Shared** — DTOs/request-response models, repository interfaces and
  shared services (BBCode parsing, rank calculation, word filtering, password
  policy validation, image path/signature checks) used by both the App and API.
- **MvcBB.ModKit** — the contract for drop-in mods (`IApiMod`, `IAppMod`,
  `ModLoader`). Mods are built in their own separate solutions against this
  assembly and never live in this one.
- **MvcBB.API.InMemory / MvcBB.API.SQL / MvcBB.API.PostgreSql** — three
  interchangeable backend implementations behind the same repository
  interfaces, selected via the `DataProvider` setting in
  `MvcBB.API/appsettings.json`. **InMemory is the default and the only
  backend this app is actually run and tested against day to day.** The SQL
  Server and PostgreSQL implementations are written for architectural
  completeness and call stored procedures/functions that live in
  `Database/SQL` and `Database/PostGresSql` (see those folders' install
  scripts) — they haven't been exercised against a live PostgreSQL
  instance beyond schema/proc smoke tests.

## Functionality

### Forums
- Boards, organized into nested categories (unlimited depth), each optionally
  marked private (hidden from anyone not signed in).
- Threads: create, reply, edit, lock, sticky, delete (soft-delete with a
  reason, author attribution preserved).
- Posts: BBCode formatting with a toolbar and live preview, smilies, a single
  attached image, quoting another post, edit history, like/unlike.
- Thread polls: single-choice, attached only at thread-creation time, with
  live vote counts and results.
- Thread statistics: reply count, view count, total likes, participant count,
  last poster — shown on every thread listing (home page, board index, board
  details) via a shared component, and on the thread itself. Likes and
  participant counts are maintained as stored counters, updated inline as
  likes/replies happen, rather than recomputed on every page load.
- Thread "watch"/subscribe: opt in to notifications on new replies, choosing
  private-message and/or email delivery independently.
- Thread activity-status icon (hot/active/cool/dormant/none), shown next to
  the thread title on both the thread page and board listings, based on reply
  count and time since last activity.
- Sticky, locked and imported-content markers shown before a thread title,
  shared by the board listings and the home page.
- Board categories can be collapsed/expanded from the board listing page,
  with the collapsed state remembered per-browser.
- Boards load their list of threads up front; each board's most recent
  threads are fetched on demand via a "show recent threads" button, so a
  board index with many boards no longer waits on every board's threads to
  load before the page renders.
- Inactive boards are hidden from everyone except Administrators.
- One shared visibility rule decides who can see every board, thread and
  post (active/private boards, deleted content), so every page and API
  endpoint agrees.
- Posts/threads imported from a legacy forum are flagged and shown with a
  small icon, to distinguish migrated content from content created natively.

### Home page
- **Latest Discussions**: the 10 most recently active threads across every
  board the visitor can see, ordered by last post (then creation date), each
  showing its sticky/locked/imported markers, activity icon, reply count,
  starter, board, and last poster with the time of the last post.
- Forum statistics (members, threads, posts, active users in the last 7
  days) and a "Newest Members" list, all computed as counts/sorted pages at
  the data layer rather than by loading every member or thread. The post
  total comes from each board's stored post count, so it reflects the whole
  forum rather than just the threads on screen.
- Quick links to Boards, Members, Team and Search, plus the admin-set site
  title and welcome message.

### Members & profiles
- Registration, login, password reset (emailed link — logged to console in
  the absence of a configured SMTP server), profile editing, avatar upload.
- Optional **Cloudflare Turnstile** challenge on the registration form,
  switched on by setting a Turnstile site key and secret key.
- Registration can be turned off entirely from the admin settings, leaving
  account creation to admins only.
- Post-count-based **ranks** (admin-configurable ladder of titles, e.g.
  "Newbie" → "Forum God"), computed live from each member's post count.
- Admin-only **custom titles** that override a member's computed rank
  entirely, for anyone the admin wants to give a unique title to.
- Member directory (sortable/searchable) and a "Team" page listing
  Administrators/Moderators.
- Private messaging (PMs) between members, with an on-screen toast alert and
  a live-updating unread-count badge for new messages.

### Moderation & content tools
- Three roles (User, Moderator, Administrator) plus a `Deleted` state used
  for anonymizing removed accounts (existing posts/threads keep a generic
  author rather than being deleted outright). Administrators and Moderators
  are marked with a role icon next to their username on every post, and
  role colour (red/green/blue/grey) is consistent across the member
  directory and profile pages.
- Content reporting (posts, threads, users) with an admin review queue.
- Site-wide search across threads, posts and members.
- Admin-managed **word filter**: a censor list applied to post content and
  thread titles at write time (so stored data is already clean, not filtered
  only at render time).
- Admin-managed BBCode tag reference and smilie library.

### Site administration
- Admin dashboard linking every management area: Announcements, BBCode,
  Boards, Ranks, Reports, Settings, Smilies, Users, Word Filter, plus Mods.
- **Announcements**: site-wide banners shown on every page, each with an
  active flag, sort order and optional start/end dates.
- Display settings: site name/title/logo, welcome message, threads-per-page,
  site-wide date/time display format, an optional centered footer
  disclaimer, and whether public registration is open.
- **Password policy**: admin-set minimum length and required character
  classes (upper/lower case, digit, special character), applied to
  registrations, admin-created accounts, password changes and resets, plus a
  one-click "force password reset for all users".
- User management: create accounts, role changes, custom titles, email
  changes, timed or indefinite account locks, "must change password on next
  login", and account anonymization.
- **Mods** page: lists every mod found on disk with its version and
  compatibility status, and lets an admin enable or disable it.

### Mods
- Mods are plain DLLs built against `MvcBB.ModKit` and dropped into a `Mods`
  folder next to the API's and App's build output — that's the entire
  installation step; core code never changes for a new mod.
- The API-side assembly (`IApiMod`) registers its own services and
  controllers, declares its version and the range of core versions it
  supports, and installs/upgrades its own database schema on startup only
  when its version changes.
- The App-side assembly (`IAppMod`) is a Razor Class Library contributing
  controllers, precompiled views and navbar links. A disabled or
  incompatible mod's pages and nav links are switched off without being
  removed.
- **View slots** (ModKit 1.1.0) let an App-side mod render its own view
  components into named places on core pages: under each post's author
  (`post.author`), at the foot of each post (`post.footer`), under the avatar
  on a profile (`profile.sidebar`), under Quick Links on the home page
  (`home.sidebar`), and in the page `<head>` for stylesheets (`layout.head`).
  Slot contexts carry every member and post id on the page, so a mod can load
  data for a whole thread in one call. A disabled mod's slots render nothing,
  and a failing one is logged and skipped. See `docs/ModKit-ViewSlots.md`.
- A mod can embed static files (CSS, images) in its App-side DLL; they're
  served at `/_mods/{modId}/...` while the mod is enabled.

### Appearance
- Light and dark theme, toggled from the navbar and remembered per-browser
  (no account setting), with no flash-of-wrong-theme on load.
- Footer shows the running app's version (read from the build's assembly
  version) next to the site name, plus the optional admin-set disclaimer.

### Security
- **Signed in by default**: every API endpoint requires an authenticated
  caller unless it's explicitly marked public (login, registration, password
  reset, the service token, and read-only browsing for guests).
- **Role-based admin actions**: management endpoints and pages are limited to
  Administrators (or Moderators where appropriate). The App's shared service
  token is read-only, so any change has to be made with the signed-in user's
  own token.
- **Separate token kinds**: user tokens and the App's service token are
  signed with different keys and audiences, with issuer/audience checks, so
  one can never be passed off as the other.
- **Session revocation**: a user's existing sessions end straight away when
  their password changes, their role changes or their account is locked.
  Every request re-checks the token against the account, both in the API and
  for the App's sign-in cookie.
- **BBCode XSS protection**: post content is HTML-encoded before BBCode is
  applied, and link/image URLs are limited to http(s), mailto and
  site-relative paths (no `javascript:`, `data:` and the like).
- **Safe uploads and deletes**: uploaded images are checked by their actual
  bytes (PNG/JPEG/GIF only) rather than the file name or content type, saved
  under a generated name, and only files the app itself created are ever
  deleted, so a crafted path can't reach anything outside the images folder.
- **Security headers**: Content-Security-Policy, X-Content-Type-Options,
  X-Frame-Options, Referrer-Policy and Permissions-Policy on the App
  (uploaded files included), and nosniff/no-framing headers on the API.
- Forced password change on next login, enforced on every page until done.

### Hosting
- The API's root URL is a bare status page showing whether it can reach its
  configured database (HTTP 200 when healthy, 503 when not).
- Real email delivery via SMTP once `Smtp:Host` is set; until then emails
  are written to the log instead.

## Projects

| Project | Purpose |
|---|---|
| `MvcBB.App` | Public MVC web app |
| `MvcBB.API` | Backend API (auth, business logic) |
| `MvcBB.Shared` | Shared models/DTOs/repository interfaces/services |
| `MvcBB.ModKit` | Contract and loader for drop-in mods |
| `MvcBB.API.InMemory` | In-memory repository implementation (default) |
| `MvcBB.API.SQL` | SQL Server repository implementation |
| `MvcBB.API.PostgreSql` | PostgreSQL repository implementation |

## Running locally

Both `MvcBB.App` and `MvcBB.API` need to run together — the App has no
direct database access and calls the API over HTTP. With the default
`InMemory` provider, no database setup is required; an admin account
(`admin` / `Admin123!`) is seeded automatically on first run.

### Configuration

- `MvcBB.API/appsettings.json`: `DataProvider` (`InMemory`, `Sql` or
  `PostgreSql`) and the matching connection string, `Jwt:SecretKey`, the
  `ApiClient` id/secret, `Cors:AllowedOrigins` (the App's URLs), and
  optional `Smtp` settings.
- `MvcBB.App/appsettings.json`: `Api:BaseUrl`, the matching `Api` client
  id/secret, and optional `Turnstile` keys.
- For SQL Server or PostgreSQL, deploy the schema first with the
  `install.ps1` script in `Database/SQL` or `Database/PostGresSql`.

Change the placeholder JWT key and client secret before deploying anywhere
public.

## Release history

### v1.0.0

First release. Everything described above, including:

- Forums: nested board categories, private and inactive boards, threads with
  lock/sticky/soft-delete, BBCode posts with images, quotes, edit history and
  likes, polls, thread subscriptions by PM and/or email, activity icons and
  stored thread statistics.
- Home page Latest Discussions ordered by most recent activity, with thread
  markers, last poster details and accurate forum-wide statistics.
- Members: registration (optional Turnstile, can be closed), password reset,
  profiles and avatars, post-count ranks, custom titles, member directory,
  Team page, and private messages with live unread alerts.
- Moderation: roles with account anonymization, content reports, search,
  write-time word filter, BBCode and smilie management.
- Administration: dashboard, site-wide announcements, display settings,
  password policy with forced resets, and full user management including
  account locks and admin-created accounts.
- Mod framework (`MvcBB.ModKit`) with drop-in install, version
  compatibility checks, self-installing schema and admin enable/disable.
- Three data providers (InMemory, SQL Server, PostgreSQL) behind one set of
  repository interfaces, with an API database status page.
- Light/dark theme and app version in the footer.
- Security hardening: signed-in-by-default API, role-based admin actions,
  read-only service token, separate user/service token keys, session
  revocation on lock/demote/password change, BBCode XSS protection, safe
  upload and delete paths, and browser security headers.
