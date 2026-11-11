# MvcBB

A modern bulletin-board (forum) system built on ASP.NET Core, structured as a
two-tier MVC + API application with a swappable persistence layer.

## Architecture

- **MvcBB.App** — the public-facing MVC web app (Razor views, controllers).
  Talks to `MvcBB.API` over HTTP/JWT; holds no direct database access itself.
- **MvcBB.API** — the backend API. Issues JWTs (per-user login tokens and a
  client-credentials token for anonymous/service calls), hosts all business
  logic, and delegates persistence to whichever repository implementation is
  configured.
- **MvcBB.Shared** — DTOs/request-response models and repository interfaces
  shared between the App and API.
- **MvcBB.API.InMemory / MvcBB.API.SQL / MvcBB.API.PostgreSql** — three
  interchangeable backend implementations behind the same repository
  interfaces, selected via the `DataProvider` setting in
  `MvcBB.API/appsettings.json`. **InMemory is the default and the only
  backend this app is actually run and tested against day to day.** The SQL
  Server and PostgreSQL implementations are written for architectural
  completeness and call stored procedures/functions that live in
  `Database/SQL` and `Database/PostGresSql` (see those folders' install
  scripts) — they haven't been exercised against a live SQL Server/PostgreSQL
  instance beyond schema/proc smoke tests.

## Functionality

### Forums
- Boards, organized into nested categories (unlimited depth), each optionally
  marked private (hidden from anyone not signed in).
- Threads: create, reply, edit, lock, sticky, delete (soft-delete with a
  reason, author attribution preserved).
- Posts: BBCode formatting with a live preview, smilies, a single attached
  image, quoting another post, edit history, like/unlike.
- Thread polls: single-choice, attached only at thread-creation time, with
  live vote counts and results.
- Thread statistics: reply count, view count, total likes, participant count,
  last poster — shown on every thread listing (home page, board index, board
  details) via a shared component, and on the thread itself.
- Thread "watch"/subscribe: opt in to notifications on new replies, choosing
  private-message and/or email delivery independently.

### Members & profiles
- Registration, login, password reset (emailed link — logged to console in
  the absence of a configured SMTP sender), profile editing, avatar upload.
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
  author rather than being deleted outright).
- Content reporting (posts, threads, users) with an admin review queue.
- Site-wide search across threads/posts.
- Admin-managed **word filter**: a censor list applied to post content and
  thread titles at write time (so stored data is already clean, not filtered
  only at render time).
- Admin-managed BBCode tag reference and smilie library.

### Site administration
- Admin dashboard linking every management area: Users, Boards, BBCode,
  Smilies, Ranks, Word Filter, Reports, Settings.
- Display settings: site name/title/logo, welcome message, threads-per-page,
  and an optional centered footer disclaimer.
- User management: role changes, custom titles, account anonymization.

### Appearance
- Light and dark theme, toggled from the navbar and remembered per-browser
  (no account setting), with no flash-of-wrong-theme on load.
- Footer shows the running app's version (read from the build's assembly
  version) next to the site name, plus the optional admin-set disclaimer.

## Projects

| Project | Purpose |
|---|---|
| `MvcBB.App` | Public MVC web app |
| `MvcBB.API` | Backend API (auth, business logic) |
| `MvcBB.Shared` | Shared models/DTOs/repository interfaces |
| `MvcBB.API.InMemory` | In-memory repository implementation (default) |
| `MvcBB.API.SQL` | SQL Server repository implementation |
| `MvcBB.API.PostgreSql` | PostgreSQL repository implementation |

## Running locally

Both `MvcBB.App` and `MvcBB.API` need to run together — the App has no
direct database access and calls the API over HTTP. With the default
`InMemory` provider, no database setup is required; an admin account
(`admin` / `Admin123!`) is seeded automatically on first run.
