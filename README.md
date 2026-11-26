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
  scripts).

## Functionality

### Forums
- Boards, organized into nested categories (unlimited depth), each optionally
  marked private (hidden from anyone not signed in).
- Threads: create, reply, edit, lock, sticky, delete (soft-delete with a
  reason, author attribution preserved).
- Posts: BBCode formatting with a toolbar and live preview, smilies, a single
  attached image, quoting another post, edit history, like/unlike.
- **Spoilers**: `[spoiler]text[/spoiler]` or `[spoiler=Title]text[/spoiler]`
  hides text in a box the reader clicks to open. Spoiler text stays hidden in
  plain-text summaries too (notifications, recent posts).
- **@mentions**: `@Name`, or `@"Some Name"` for a username with spaces or
  other characters. The editor suggests members as you type after `@`. A
  mention of a real member links to their profile and notifies them (not
  when it's inside a quote or code block, and not again when the post is
  edited).
- **Draft autosave**: what a member types into a new discussion, reply or
  edit is saved in their browser as they go. If the page is closed, the
  session expires or posting fails, opening that editor again offers to
  restore the draft. A draft is deleted once its post has been saved, and
  drafts older than 14 days are cleared. Drafts never leave the browser.
- **Paged discussions**: a thread shows a page of posts at a time ("Posts
  per page" in Admin > Settings, default 20), paged in the database, with
  page links above and below. Links to a particular post (quotes, search
  results, recent posts, reports, notifications) open the page it's on, the
  last-post links open the last page, and a member who opens at their first
  unread post lands on whichever page that is.
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
  the absence of a configured SMTP server), profile editing, avatar upload,
  and an optional birthday (day and month, year optional).
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
- **Notification centre**: a bell in the navbar, with a live unread count,
  opens one list of everything involving the member: replies in discussions
  they started or watch, @mentions, quotes of their posts, likes, private
  messages, and moderators' decisions on their held posts. Each member gets
  one notification per post at most, never about their own posts and never
  about boards they can't see. Opening a discussion or message marks its
  notifications as read, and read notifications are cleared after 90 days.

### Moderation & content tools
- Three roles (User, Moderator, Administrator) plus a `Deleted` state used
  for anonymizing removed accounts (existing posts/threads keep a generic
  author rather than being deleted outright). Administrators and Moderators
  are marked with a role icon next to their username on every post, and
  role colour (red/green/blue/grey) is consistent across the member
  directory and profile pages.
- Content reporting (posts, threads, users) with an admin review queue.
- **Report an issue**: a bug icon next to the user menu lets any signed-in
  member report a problem with the site itself, optionally with the page
  they were on. These arrive in the same review queue as a "Site issue"
  report and follow the same status/notes workflow.
- **Open reports alert**: moderators and administrators get a navbar flag
  with a live count of reports still Pending or Investigating, plus an
  on-screen toast when a new one arrives (refreshed the same way as the
  unread PM badge).
- **Moderation queue**: optionally hold new members' posts (their first one,
  or first few, set in Moderation > Settings) until a moderator approves
  them. A held post isn't visible anywhere else. Approving publishes it as
  if it had just been posted; turning it down (with an optional reason)
  deletes it. Either way the member gets a notification. Moderators and
  administrators see a navbar badge with the number waiting.
- **Moderation log**: every action by a moderator or administrator
  (approving or turning down held posts, deleting or editing others' posts,
  locking, pinning, renaming and deleting threads, locking members, changing
  roles and custom titles, anonymizing, updating reports, changing
  moderation settings) is recorded with who, when, what and any reason.
- Site-wide search across threads, posts and members.
- Admin-managed **word filter**: a censor list applied to post content and
  thread titles at write time (so stored data is already clean, not filtered
  only at render time).
- Admin-managed BBCode tag reference and smilie library.

### Site administration
- Admin dashboard linking every management area: Announcements, BBCode,
  Boards, Email Users, Moderation, Ranks, Reports, Settings, Smilies, Users,
  Word Filter, plus Mods.
- **Announcements**: site-wide banners shown on every page, each with an
  active flag, sort order and optional start/end dates.
- Display settings: site name/title/logo, welcome message, threads-per-page,
  site-wide date/time display format, the forum's time zone, an optional
  centered footer disclaimer, and whether public registration is open.
- **Forum time zone**: every timestamp is stored in UTC and shown in the
  time zone chosen in the admin settings (daylight saving included).
  Admin-entered dates (announcement start/end, account locks) are typed in
  that zone too.
- **Password policy**: admin-set minimum length and required character
  classes (upper/lower case, digit, special character), applied to
  registrations, admin-created accounts, password changes and resets, plus a
  one-click "force password reset for all users".
- User management: create accounts, role changes, custom titles, email
  changes, timed or indefinite account locks, "must change password on next
  login", and account anonymization.
- **Email Users**: admins email all members, one role or one member
  (maintenance notices, news, alerts), from their own address or the site's
  no-reply address, with a confirmation step showing the recipient count
  and From line, a "send a test to me" button, and a send log with
  progress and the reason for any email that couldn't be sent. Sending
  keeps under your host's hourly/daily email limits (set in Settings), a
  Sending activity chart shows every email the site has sent against those
  limits, and an email cut off by a restart can be resumed for the members
  who haven't had it. Members
  can turn these emails off on their profile; an "Important service alert"
  still reaches them.
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
- **Portal home page slots** let a mod add a widget to any column of the
  home page: `home.left`, `home.center.top` (above Latest Discussions),
  `home.center` (below it) and `home.right`. `home.sidebar` still works and
  still renders under Quick Links.
- **Alert slots** let a mod alert people the way core does for new PMs and
  open reports: an icon with a count badge in the navbar (`layout.navbar`)
  and a toast that stacks with core's report toast (`layout.toasts`). The
  Feature Board mod uses them to alert Administrators to new requests.
- **Mod admin pages** (ModKit 1.3.0): an App-side mod lists its
  administration pages from `IAppMod.GetAdminItems()`, and core shows each
  one in all three admin entry points: a card on the Admin Dashboard, an item
  under "Mods" in the navbar's Admin dropdown, and a link under "Mods" in the
  admin sidebar. Administrators only, enabled mods only. The page itself is
  an action on the mod's own controller, which must carry
  `[Authorize(Roles = "Administrator")]`, and can use core's `_AdminLayout`.
  The Calendar mod uses it for its settings and categories.
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
  optional `Smtp` settings. `Smtp:FromEmail` is also the no-reply address
  admin emails are sent from.
- `AdminEmail` (API): `BatchSize` (default 50) and `BatchDelaySeconds`
  (default 30) throttle admin emails, which go out in the background in
  batches with a pause between them. `AllowAdminFromAddress` (default
  `false`) decides what "from me" means. Off, the email is sent from
  `Smtp:FromEmail` as "*admin* via *site name*", with the admin's address as
  Reply-To. On, the admin's own address is the From. Only turn it on when
  your SMTP relay is allowed to send for the admins' email domains (SPF and
  DKIM set up for it), or receiving servers will reject the mail or mark it
  as spam under DMARC, which is near certain for Gmail or Outlook addresses.
- Host sending limits, for hosts that cap how many emails a site may send
  (e.g. 250 an hour): set "Host sending limit per hour" and "per day" in
  Admin > Settings, a little below your host's limits. Each one left at 0
  there falls back to `AdminEmail:MaxPerHour` / `AdminEmail:MaxPerDay` in the
  API's appsettings.json (default 0 = no limit), so Settings wins whenever
  it's set. The limits count every email the API sends (password resets,
  notifications and the rest), not just admin emails. Admin emails wait when
  they would go over, and 5% of each limit (at least 1) is kept back so a
  big admin email never uses it all up and stops password resets; other
  emails are never held up. The count is kept in memory and refilled from
  the send log's last 24 hours when the API starts, so a restart doesn't
  hand out the limit again. If the API runs on more than one server, each
  counts only its own emails, so divide the limits between them.
- `AdminEmail:SendLogRetentionDays` (API, default 30): how many days of the
  email send log (times and kinds of email only, no addresses or subjects)
  are kept for Sending activity. Older rows are deleted daily.
- `Notifications:RetentionDays` (API, default 90, at least 7): read
  notifications older than this are deleted daily. Unread ones are kept.
- `MvcBB.App/appsettings.json`: `Api:BaseUrl`, the matching `Api` client
  id/secret, optional `Turnstile` keys, and `Navigation:MaxModLinks` (how
  many mod links the navbar shows before the rest go into a "More" menu,
  default 3).
- For SQL Server or PostgreSQL, deploy the schema first with the
  `install.ps1` script in `Database/SQL` or `Database/PostGresSql`.

Change the placeholder JWT key and client secret before deploying anywhere
public.

## Release history

### v1.4.0

New features:

- **Birthdays on profiles.** Members can add their birthday on their
  profile: day and month, with the year optional for anyone who'd rather
  not show their age. It shows in the profile's details card. Clearing all
  three removes it. Anonymizing an account removes it too. Mods can read it
  (the Calendar mod shows members' birthdays each year).
- **Mod admin pages.** A mod can list its administration pages from
  `IAppMod.GetAdminItems()` (ModKit 1.3.0), and core shows each one as a card
  on the Admin Dashboard, under "Mods" in the navbar's Admin dropdown and
  under "Mods" in the admin sidebar, for enabled mods only. Existing mods
  load and run unchanged.
- **"More" menu for mod links.** Once enabled mods add more navbar links than
  `Navigation:MaxModLinks` (App appsettings.json, default 3), the extra ones
  move into a "More" menu, so the navbar doesn't outgrow the page.
- **Spoiler tag.** `[spoiler]...[/spoiler]` and `[spoiler=Title]...[/spoiler]`,
  with a toolbar button. Closed until clicked, and hidden in plain-text
  summaries.
- **Draft autosave.** New discussions, replies and post edits are saved in
  the browser while being written and offered back if the post never made
  it (closed tab, expired session, an error). Deleted once the post is
  saved.
- **@mentions.** `@Name` / `@"Some Name"`, with member suggestions in the
  editor. Mentions of real members link to their profiles and notify them.
- **Notification centre.** A navbar bell and a Notifications page bringing
  together replies, mentions, quotes, likes, private messages and moderation
  decisions, with an unread count. Watching a thread by PM or email works as
  before, alongside it.
- **Paged discussions.** Threads now show "Posts per page" posts at a time
  (Admin > Settings, default 20) instead of every post at once, which made
  very long discussions slow to open (tens of seconds, or minutes, for
  threads with tens of thousands of posts) and produced huge pages. Only the
  posts on the page are read from the database. Links to a post, the
  last-post links, jump to first unread and notifications all open the right
  page. `GET api/posts/thread/{id}/page` is the new endpoint;
  `GET api/posts/thread/{id}` still returns every post, for anything that
  relied on it.
- **Moderation queue and log.** A new Moderation page (Admin Dashboard card,
  navbar Admin dropdown, admin sidebar; a navbar link and badge for
  moderators) with three tabs: the queue of held posts to approve or turn
  down, the moderation log of every moderator action, and (administrators
  only) the setting to hold new members' first posts, off by default.

Fixes and changes:

- **Quick Links**: the home page's Quick Links card now lists every enabled
  mod's links too, after core's own (all of them, even those in the navbar's
  "More" menu).
- **Profile errors**: when the API refused a profile change with a plain
  message (for example "Username is already taken"), the page showed
  "Failed to update profile" instead. It now shows the API's message. A
  profile form the App itself rejects now says why, too.
- **Reply errors**: when the API refuses a reply (for example because the
  thread was locked meanwhile), the page now shows why.
- After posting a reply, the page now opens at the new post.
- **Deleting a thread** failed on SQL Server and PostgreSQL for any thread
  with posts (a foreign key error), after already taking one off the board's
  thread count. It now deletes the thread with its posts, likes, poll,
  watchers and notifications in one transaction, unlinks quotes of its posts
  elsewhere, and only then lowers the board's thread and post counts.

Upgrading from v1.3.1: deploy the new build of the App and API (and rebuild
any mods; ModKit is now 1.3.0), then re-run `install.ps1` for your database.
`Upgrades/v1.4.0.sql` adds `Users.BirthDay`, `BirthMonth` and `BirthYear` /
`users.birth_day`, `birth_month` and `birth_year` (empty for existing
members), and the user procedures are recreated with them. It also adds the
Spoiler row to an already-seeded BBCode tag list and
`DisplaySettings.PostsPerPage` / `display_settings.posts_per_page` (20). The new tables
(`Notifications`, `ModerationQueue`, `ModerationLog`, `ModerationSettings` /
`notifications`, `moderation_queue`, `moderation_log`,
`moderation_settings`) are created by their `Tables/` scripts, and
moderation settings are seeded with holding switched off, so nothing changes
for members until an administrator turns it on. Existing rows aren't
touched. Optionally set `Navigation:MaxModLinks` in the App's
appsettings.json and `Notifications:RetentionDays` in the API's.

### v1.3.1

New features:

- **First unread post.** Discussions remember how far each signed-in member
  has read, saved as posts scroll into view. On the next visit, posts added
  since then get a "New" badge and an accent stripe, and the page opens at
  the first of them. A new profile setting, "Open discussions at my first
  unread post" (on by default), turns the jump off; the page then opens at
  the top with a "Jump to first unread" button instead. A member's own posts
  are never marked unread, and nothing is marked on a first visit. New API
  endpoint: `PUT api/threads/{id}/read`.
- **Back to top.** Every page gets a floating button that appears once the
  page has scrolled down.
- **Alert slots for mods.** Two new ModKit view slots, `layout.navbar` (an
  icon with a count badge in the navbar) and `layout.toasts` (a toast that
  stacks with core's open-reports toast), so a mod can alert people the way
  core does for new PMs and open reports. Both render for signed-in members
  only. See `docs/ModKit-ViewSlots.md`, section 7.2. Existing mods load and
  run unchanged.

Fixes and changes:

- **Admin menus**: the navbar's Admin dropdown now has Sending activity, and
  the admin Dashboard has a Mods card.
- **Sending activity chart**: axis labels are larger and use the page's own
  text colour, so they're readable in the dark scheme, and the grid lines
  show up better there. The vertical axis has five steps instead of four.
- **Dark scheme toasts**: the new-PM and open-reports toasts (and any a mod
  adds) were a light panel with light text in the dark scheme. They now use
  the dark scheme's colours.

Upgrading from v1.3.0: deploy the new build of the App and API (and
rebuild any mods; ModKit is now 1.2.0), then re-run `install.ps1` for your
database. It creates the new `ThreadReads` / `thread_reads` table and its
procedures, and `Upgrades/v1.3.1.sql` adds `Users.AutoJumpToUnread` /
`users.auto_jump_to_unread` (on for existing members). Existing rows
aren't touched. No settings changes.

### v1.3.0

New features:

- **Failed recipients.** An admin email's details page lists the members it
  couldn't be sent to, with the reason for each (the mail server's message)
  and when it happened, instead of pointing at the API log.
- **Host sending limits.** For hosts that cap how many emails a site may
  send (e.g. 250 an hour), set "Host sending limit per hour" and "per day"
  in Admin > Settings, or `AdminEmail:MaxPerHour` / `AdminEmail:MaxPerDay`
  in the API's appsettings.json (Settings wins when set). Every email the
  site sends counts, but only admin emails wait for them, and 5% of each
  limit (at least 1) is kept back for password resets and other site
  emails. A waiting email's details page shows when sending carries on. The
  confirmation step shows the last hour's usage and, for a large email, how
  many can go now and roughly how long the rest will take, counting what's
  already been sent recently.
- **Resume interrupted emails.** Each member an email reaches is recorded.
  An email cut off by an API restart is still marked *Interrupted* and isn't
  restarted on its own, but its details page now has a "Resume sending"
  button that sends it only to the members who haven't had it yet,
  retrying any it failed for. Who that is gets worked out again when it
  resumes: members who joined since are included, and members who opted
  out since are left out (unless it's an important alert).
- **Sending activity** (Admin > Email Users > Sending activity). A chart of
  every email the site has sent over the last 24 hours, 7 days or 30 days,
  coloured by kind (Email Users, notifications, password resets,
  registration, other), with the host limit drawn across it as a ruler and
  a dotted line where admin emails stop. Bars over the limit are shown in
  red, and the 24-hour view adds a rolling 60-minute line, since hosts
  usually count any 60 minutes rather than clock hours. Above it, bars show
  how much of the hourly and daily limit is used and how many admin emails
  could go now, or when the next one can. The admin Dashboard's Email Users
  card shows the last hour's usage. Each send is logged with just its time
  and kind (no addresses or subjects) and kept for
  `AdminEmail:SendLogRetentionDays` (default 30). After an API restart the
  sending limits pick up from this log.
- New API endpoints: `api/admin-emails/{id}/failures`,
  `api/admin-emails/{id}/resume` (GET for the count, POST to resume),
  `api/admin-emails/usage` and `api/admin-emails/activity` (Administrator
  only).

Fixes:

- **Email preview spacing**: the email body on the confirmation step and an
  email's details page now shows blank lines and runs of spaces exactly as
  they'll be sent.
- **PostgreSQL**:
  - `install.ps1 -CreateDatabase` now creates the database when it doesn't
    exist; before, it skipped the create and failed on the first script.
  - A fresh install no longer fails on `03_threads.sql` / `04_posts.sql`,
    whose one-time backfills read tables that aren't created until later.
  - `install.ps1` runs on Windows PowerShell 5.1 as well as PowerShell 7.
  - The insert procedures that return a new id no longer give their other
    parameters defaults, which PostgreSQL rejects, so they install again;
    creating a post, which left out the id placeholder, works again too.
  - Re-running `install.ps1` on an existing database works:
    `fn_role_name` is replaced in place rather than dropped, which failed
    because `vw_post_details` depends on it.
  - Registering, logging in and the other username/email lookups work: the
    API now passes those values as `citext`, which the lookup functions
    expect.

Upgrading from v1.2.0: deploy the new build of the App and API (and
rebuild any mods), then re-run `install.ps1` for your database. It creates
the three new tables (`AdminEmailFailures`, `AdminEmailDeliveries`,
`EmailSendLog` / `admin_email_failures`, `admin_email_deliveries`,
`email_send_log`) and their procedures, and `Upgrades/v1.3.0.sql` adds
`AdminEmails.PausedUntil` and two columns to the display settings table
(`MaxEmailsPerHour`, `MaxEmailsPerDay`, both 0) / `admin_emails.paused_until`,
`max_emails_per_hour`, `max_emails_per_day`. Existing rows aren't touched.
On PostgreSQL this also installs the fixed procedures and functions above.
New API settings, all optional: `AdminEmail:MaxPerHour`,
`AdminEmail:MaxPerDay` (default 0 = no limit) and
`AdminEmail:SendLogRetentionDays` (default 30); see Configuration.

### v1.2.0

New features:

- **Email Users** (Admin > Email Users, and "Email this user" on a user's
  admin page). An admin writes a plain-text email (`{username}` is filled
  in per member) to all members, members with one role, or one member,
  and picks "from me" or "from the site's no-reply address". A
  confirmation step shows how many members it will reach, how many are
  left out because they opted out, and the exact From and Reply-To. Sending
  queues it: the API sends it in the background in throttled batches
  (`AdminEmail:BatchSize`, `AdminEmail:BatchDelaySeconds`) and records
  progress in a send log. An email cut off by an API restart is marked
  *Interrupted* and not resumed, so nobody gets it twice. Deleted
  (anonymized) accounts are never emailed. New API endpoints under
  `api/admin-emails` (Administrator only).
- **Opt-out.** Members get a "Receive emails from the forum team" tick box
  on their profile (on by default), and every admin email ends with a line
  saying how to turn it off. Ticking "Important service alert" sends to
  opted-out members too, with a line explaining why.

Upgrading from v1.1.1: deploy the new build of the App and API (and
rebuild any mods), then re-run `install.ps1` for your database. It
creates the two new tables (`AdminEmailOptOuts`, `AdminEmails` / `admin_email_opt_outs`,
`admin_emails`) and their procedures. Existing rows aren't touched. Set
`Smtp:FromEmail` to a no-reply address on your own domain, and see
`AdminEmail:AllowAdminFromAddress` under Configuration.

### v1.1.1

Fixes and changes:

- **Line breaks in posts and messages**: line breaks and blank lines typed
  into a text box were lost on display, so paragraphs ran together into one
  block. The BBCode renderer (`CoreBBCodeService.ParseBBCode`) now turns
  each line break into `<br />`, which covers posts, quotes, private
  messages, signatures, bios, announcements, reported message text, the
  editor preview and any mod that renders through `IBBCodeService`. Text in
  `[code]` blocks keeps its own line breaks as before, and a single line
  break right next to a `[quote]`, `[code]`, `[center]` or `[right]` tag is
  dropped, so tags written on their own lines don't add blank lines.
- **Line breaks in plain-text fields**: report reasons, moderator notes, the
  welcome message and the footer disclaimer, which aren't BBCode, now keep
  their line breaks too (new `.text-preserve-lines` CSS class).
- **Profile recent posts**: the one-line post preview on a member's profile
  now shows the post as plain text, without BBCode formatting.

Upgrading from v1.1.0: deploy the new build of the App and API (and
rebuild any mods). No database or settings changes.

### v1.1.0

New features:

- **Report an issue.** Signed-in members get a bug icon (tooltip "Report an
  issue") next to the user menu in the navbar. It opens a short form for a
  description, with a tick box to include the page they were on. The report
  goes into the existing admin report queue as a new **Site issue** report
  type (`ReportType.SiteIssue`, stored as type `4`) with no post, thread,
  message or member behind it, and is handled with the same status, notes
  and resolve workflow as any other report. The report list and details show
  it as "Site issue" with a link to the page (if included) instead of a
  content link. Only site-relative page addresses are stored.
- **Open reports alert.** Moderators and administrators see a flag icon in
  the navbar with a badge counting reports that are still Pending or
  Investigating, linking to the report queue. Like the unread PM badge, it's
  rendered with the page, polled every 30 seconds, and shows a toast when
  the count goes up. New API endpoint: `GET api/reports/open-count`
  (Moderator/Administrator only); `GET api/reports/stats` now also counts
  user and site issue reports.
- **Forum time zone.** New **Time Zone** setting under Admin > Settings,
  listing every time zone the server knows. The choice is stored as an IANA
  id (e.g. `Europe/London`), which works on Windows and Linux hosts. Every
  date on the site is converted from UTC to that zone for display (one
  shared helper, `DateFormatHtmlHelperExtensions`, backed by
  `MvcBB.Shared.Services.ForumTimeZone`), and announcement start/end dates
  and account lock times are entered in it and converted to UTC before
  they're saved. **Default: blank, meaning the web server's own local time
  zone**, which is how dates were shown before, so an upgraded forum looks
  the same until an admin picks a zone.
- **Portal home page slots.** Four new ModKit view slots on the home page:
  `home.left` (left column, under Quick Links), `home.center.top` (centre,
  above Latest Discussions), `home.center` (centre, below it) and
  `home.right` (right column, under Statistics/Newest Members), all with
  `PageSlotContext`. The existing `home.sidebar` slot is unchanged and still
  renders under Quick Links, just before `home.left`. Empty slots render no
  markup, so the page is unchanged without mods. See section 7.1 of
  `docs/ModKit-ViewSlots.md`. Existing mods load and run unchanged.

Fixes and changes:

- **Post report links**: on the report details page, a reported post now
  opens its thread at that post. Previously the post's id was used as a
  thread id, opening the wrong thread or none. A thread or post that no
  longer exists is shown as unavailable instead of a broken link.
- **Message report links**: message reports no longer link to the message
  page, which only the sender and recipient can open.
- **Reported message text**: moderators and administrators can read the
  text of a reported private message on the report details page (and only
  there), with a note that it's shown because the message was reported.
  Normal message privacy is unchanged: the message itself still only opens
  for its sender and recipient.
- **Who can report a message**: only the recipient of a private message can
  report it. The Report button shows only for them; anyone else gets "not
  found", so message ids can't be probed.
- **User reports on SQL Server and PostgreSQL**: reports about a member
  (type `3`) were rejected because the reports table's check constraint and
  content-exists function only knew types 0 to 2. Both now accept types 0
  to 4.

Settings and data:

- `DisplaySettings` gains `TimeZoneId` (blank = server local time).
- Reports gain `PageUrl` (site issue reports only).
- Timestamps were already written in UTC by every provider (`DateTime.UtcNow`
  in the API, `SYSUTCDATETIME()` on SQL Server, `timestamptz` on
  PostgreSQL), so **existing data needs no migration** and none is
  rewritten. The one exception is dates typed into admin forms before
  v1.1.0 (announcement start/end dates and account "locked until" times):
  those were saved exactly as typed and are now read as UTC, so on a server
  that isn't on UTC they'll show shifted by the zone's offset. Check any
  scheduled announcements and timed locks after upgrading and re-save them
  if needed.

Upgrading from v1.0.0:

1. Stop the App and API, deploy the new build of both (and any mods).
2. **SQL Server / PostgreSQL**: re-run `install.ps1` in `Database/SQL` or
   `Database/PostGresSql` against the existing database. It now runs the
   guarded `Upgrades/v1.1.0.sql` straight after the table scripts (adds the
   new columns and widens the report type check), then recreates the
   functions and procedures. It's safe to re-run and never drops data. The
   upgrade script can also be run on its own first (see its header).
   **InMemory**: nothing to do.
3. Start the API, then the App, and pick a time zone under Admin > Settings
   if the server's local zone isn't the one the forum should use.

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
