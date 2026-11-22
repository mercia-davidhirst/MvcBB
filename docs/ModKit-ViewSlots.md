# ModKit view slots: design

Status: **implemented** in `MvcBB.ModKit` 1.1.0 and MvcBB.App (MvcBB v1.1.0).
Sections 3 to 7 and 11 describe what was built; where it differs from the
original proposal, the text says so. Section 7.1 covers the portal home page
slots (`home.left`, `home.center.top`, `home.center`, `home.right`) added
later in the same release.

## 1. Problem

Today an App-side mod (`IAppMod`) can do two things: register services and
add navbar links. It can ship its own controllers and pages, but it cannot
add anything to a *core* page. A mod that wants to show something next to a
member's name, such as badges, has no way in short of editing core views,
which defeats the drop-in model.

Overriding whole core views is not the answer either. It is all-or-nothing,
two mods can't both override the same view, and every core view change
silently breaks the override.

## 2. Proposal in one paragraph

Core views mark a small number of **named slots**, such as `post.author`
or `profile.sidebar`. An App-side mod declares, from `IAppMod`, which of
its **view components** render into which slot. At render time, a core tag
helper (`<mod-slot>`) looks up the contributions for that slot, keeps only
those from currently enabled mods, invokes each view component with a
small, read-only **slot context**, and writes the output in order. If no
enabled mod contributes, the slot renders nothing at all, so core markup is
unchanged for a site with no mods.

## 3. API shape (MvcBB.ModKit)

All new types are plain C#. ModKit keeps its current dependency footprint
(DI and configuration abstractions only) and does not take a reference on
ASP.NET Core MVC. The view component is referenced by `Type`, and the host
checks it at startup.

### 3.1 Slot names

```csharp
namespace MvcBB.ModKit
{
    /// <summary>Slot names core views expose. Strings, so a mod built against
    /// a newer ModKit can name a slot an older core doesn't have yet; the host
    /// logs and ignores unknown names instead of failing.</summary>
    public static class CoreSlots
    {
        public const string PostAuthor     = "post.author";
        public const string PostFooter     = "post.footer";
        public const string ProfileSidebar = "profile.sidebar";
        public const string HomeSidebar    = "home.sidebar";
        public const string HomeLeft       = "home.left";
        public const string HomeCenterTop  = "home.center.top";
        public const string HomeCenter     = "home.center";
        public const string HomeRight      = "home.right";
        public const string LayoutHead     = "layout.head";
    }
}
```

`home.sidebar` and `layout.head` were added to the first set during
implementation (see section 7).

### 3.2 Contributions

```csharp
namespace MvcBB.ModKit
{
    /// <param name="Slot">One of CoreSlots.</param>
    /// <param name="ViewComponentType">A ViewComponent subclass in the mod's
    /// own assembly. Invoked by type, never by name, so two mods (or a mod
    /// and core) using the same class name never collide.</param>
    /// <param name="Order">Lower renders first. Ties are broken by mod Id so
    /// the order is stable across restarts.</param>
    public sealed class ModSlotContribution
    {
        public ModSlotContribution(string slot, Type viewComponentType, int order = 0);
        public string Slot { get; }
        public Type ViewComponentType { get; }
        public int Order { get; }
    }
}
```

Built as a class rather than a positional record, for the same binary
compatibility reason as the contexts below.

`IAppMod` gains one member, with a default implementation:

```csharp
public interface IAppMod
{
    // ...existing members unchanged...

    /// <summary>View components this mod renders into core view slots.
    /// Filtered by the enabled-mod set at render time, exactly like
    /// GetNavItems.</summary>
    IEnumerable<ModSlotContribution> GetSlotContributions() => Array.Empty<ModSlotContribution>();
}
```

The default body matters. Adding a plain interface member would make every
existing mod DLL (Gallery 1.1.0 included) fail to load with a
`TypeLoadException`, because the compiled class doesn't implement it. With a
default interface method, existing mods load unchanged and simply
contribute nothing. This keeps the change additive, so the ModKit bump is
1.0.0 to 1.1.0 and existing mods' `MinCoreVersion = "1.0.0"` still holds.

### 3.3 Slot contexts

Each slot passes one context object to the view component's
`InvokeAsync(TContext context)`. Contexts carry ids and display names only.
They deliberately do **not** hand the mod core's full models
(`UserResponse` has email and `ShowEmail`, `PostResponse` has deleted-post
content and moderator fields), so a mod can't leak those by accident and
core can change its models without breaking mods.

As built (`MvcBB.ModKit/SlotContexts.cs`), they're classes with `init`
properties, sharing a base class:

```csharp
namespace MvcBB.ModKit
{
    /// <summary>Everything a slot on this page will be asked about, so a mod
    /// can load data for all of it in one call (see section 6).</summary>
    public sealed class SlotPage
    {
        public static readonly SlotPage Empty;
        public IReadOnlyCollection<string> UserIds { get; init; }   // distinct
        public IReadOnlyCollection<int> PostIds { get; init; }
    }

    public abstract class SlotContext
    {
        public string? ViewerUserId { get; init; }   // null for guests
        public SlotPage Page { get; init; }
        public bool ViewerIsSignedIn { get; }
    }

    // home.sidebar, layout.head
    public sealed class PageSlotContext : SlotContext { }

    // profile.sidebar
    public sealed class UserSlotContext : SlotContext
    {
        public string UserId { get; init; }
        public string Username { get; init; }
        public bool IsViewerOwnProfile { get; }
    }

    // post.author, post.footer
    public sealed class PostSlotContext : SlotContext
    {
        public int PostId { get; init; }
        public int ThreadId { get; init; }
        public int BoardId { get; init; }
        public string AuthorUserId { get; init; }
        public string AuthorUsername { get; init; }
    }
}
```

`ViewerUserId` was added to every context so a mod can decide what guests
see (see section 10) and offer an "edit" link on the viewer's own profile
without reaching into `HttpContext`.

Rule for adding fields later: only add, never rename or remove (same rule as
any public ModKit type). That's why these are classes with `init`
properties: a positional record can't gain a parameter without breaking
binary compatibility.

### 3.4 Batching helper

```csharp
namespace MvcBB.ModKit
{
    /// <summary>Per-request memoised bulk lookup. Register it (or a class
    /// wrapping it) as Scoped. The first GetAsync on a page loads every key
    /// in the page set at once; later calls are served from memory.</summary>
    public sealed class SlotBatch<TKey, TValue> where TKey : notnull
    {
        public SlotBatch(Func<IReadOnlyCollection<TKey>, CancellationToken, Task<IReadOnlyDictionary<TKey, TValue>>> loadMany);

        /// <param name="pageKeys">Normally context.Page.UserIds or .PostIds.
        /// Keys already loaded are skipped; any key not in pageKeys is loaded
        /// on its own as a fallback.</param>
        public Task<TValue?> GetAsync(TKey key, IReadOnlyCollection<TKey>? pageKeys, CancellationToken ct = default);
    }
}
```

Keys the loader doesn't return are remembered as "no value" for the rest of
the request, so a member with nothing to show isn't asked about again on
every post. Overlapping first calls share one in-flight load. If a load
throws, every `GetAsync` waiting on it throws too, so a mod's loader should
catch and log its own failures and return an empty dictionary (the clans
mod does), rather than have the tag helper log one error per post.

## 4. Registration (MvcBB.App)

In `Program.cs`, right after the existing mod discovery:

1. Build a singleton `ModSlotRegistry` from `appMods`: for each mod, call
   `GetSlotContributions()` inside a try/catch.
2. Validate each contribution:
   - `ViewComponentType` must derive from `ViewComponent` (or carry
     `[ViewComponent]`) and live in that mod's own assembly. Otherwise log an
     error naming the mod and drop that contribution.
   - It must be a public, non-abstract class with exactly one public
     `InvokeAsync` or `Invoke` method, taking one parameter named `context`
     that accepts the slot's context type. Checked at startup so a mismatch
     is a clear log line rather than a failure on every page view.
   - Unknown slot name: log a warning and drop it.
   - A mod that throws from `GetSlotContributions()`: log, and that mod
     contributes nothing. It still loads and its pages still work.
3. Store contributions as `slot -> list of (ModId, Type, Order)`, sorted
   once at startup.

A broken contribution never stops the site from starting. That matches how
an incompatible mod is handled today: switched off, not fatal.

As built: `MvcBB.App/Mods/ModSlotRegistry.cs`, registered as a singleton in
`Program.cs` and resolved straight after `builder.Build()` so its log lines
(one per accepted contribution, one per rejected one) appear at startup.

The view components themselves are found through the application parts
`Program.cs` already adds for each mod's assembly, so no extra wiring is
needed for discovery.

## 5. Rendering

### 5.1 In core views

A tag helper, registered in `_ViewImports.cshtml`:

```cshtml
<mod-slot name="@CoreSlots.PostAuthor" context="@postSlot" />
```

### 5.2 What the tag helper does

1. Look up the slot in `ModSlotRegistry`. **No contributions: suppress
   output and return.** No enabled-set lookup and no allocation, so a site
   with no slot-using mods pays nothing.
2. Get the enabled mod set from `IModsService.GetEnabledModIdsAsync()`
   (already cached 10s in memory), memoised in `HttpContext.Items` so a
   thread page with 200 posts reads it once.
3. For each enabled contribution, in order, call
   `IViewComponentHelper.InvokeAsync(type, new { context })` inside a
   try/catch.
   - On success, wrap the output in
     `<div class="mod-slot-item" data-mod="{modId}">`, inside one
     `<div class="mod-slot mod-slot-{slot-name}">` per slot (dots become
     dashes, e.g. `mod-slot-post-author`), so themes can target it and it's
     clear in the page source which mod rendered what. `layout.head` gets no
     wrapper at all, since `<div>` isn't allowed in `<head>`. (No slot turned
     out to need an inline `<span>` wrapper, so that variant wasn't built.)
   - On exception, log it with the mod Id, slot and duration, and render
     nothing for that contribution. Other mods in the same slot still
     render. Visitors never see a stack trace or a broken page because a mod
     failed.
4. Time each invocation. Over a threshold (50 ms per call by default,
   `Mods:SlowSlotMilliseconds` in MvcBB.App's configuration), log a warning
   with the mod Id, so a slow mod is easy to identify.
5. If every contribution rendered nothing (empty output), write no wrapper
   at all, so empty slots don't leave stray margins.

### 5.3 Building the page set in a view

Built once, before the loop, by the core view (or its controller):

```cshtml
@{
    var slotPage = new SlotPage
    {
        UserIds = posts.Select(p => p.CreatedByUserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList(),
        PostIds = posts.Select(p => p.Id).ToList()
    };
}
@foreach (var post in posts)
{
    var postSlot = new PostSlotContext
    {
        PostId = post.Id, ThreadId = Model.Id, BoardId = Model.BoardId,
        AuthorUserId = post.CreatedByUserId, AuthorUsername = post.CreatedByUsername,
        ViewerUserId = currentUserId, Page = slotPage
    };
    ...
    <mod-slot name="@CoreSlots.PostAuthor" context="@postSlot" />
}
```

## 6. Data and performance

**The thread page is the hot spot.** `Threads/Details.cshtml` renders every
post in the thread with no paging, and imported legacy threads can be long.
Two post slots times several hundred posts, with a naive "one API call per
view component call", is several hundred HTTP round trips from MvcBB.App to
a mod's API per page view. That's the main thing this design has to
prevent.

How it's prevented:

- **The page set travels with every context.** `SlotPage` lists every user
  and post id on the page. The first call loads all of them in one request;
  every later call on that page is a dictionary lookup. `SlotBatch` makes
  this the easy path, and the reference badges mod (section 9) uses it.
- **Keys are de-duplicated.** A thread where the same 5 people wrote 300
  posts yields 5 user ids, not 300.
- **Mods should cache across requests** where the data allows. Badges change
  rarely, so a 30 to 60 second `IMemoryCache` entry per user id is
  reasonable. That's the mod's call, not core's.
- **Mod API endpoints should be bulk** (`GET api/badges/users?ids=a,b,c`),
  with a cap on the number of ids per call. The batch helper splits larger
  sets into chunks.
- **Zero cost when unused,** as in 5.2 step 1.
- **HttpClient timeouts.** A mod's named HttpClient should set a short
  timeout (2 to 3 seconds) so a hung mod API delays a page by seconds, not
  the 100-second default. Core can't enforce this for the mod, but the
  ModKit docs and the reference mod should.

Why not a separate "preload" interface that core calls before rendering?
It adds a second lifecycle hook each mod has to implement correctly, and
the page set in the context gives the same one-call-per-page result with
less to get wrong. If a slot ever needs data that can't be expressed as a
key set, a preload hook can be added then.

Rendering stays sequential (Razor renders top to bottom), so `SlotBatch`
doesn't need to handle concurrent first calls. It should still be safe if
two calls do race (a `Lazy<Task>` per load is enough).

## 7. First slots to offer

The proposal started with three; five were built, because the first real
user (the clans mod) needed a home page widget and a way to load its
stylesheet. Each slot documents how much room it has. A mod that ignores
that will look broken, and that's on the mod.

| Slot | Page and position | Context | Layout |
| --- | --- | --- | --- |
| `post.author` | Thread view, in the post header's author block, under the name and rank title (so, beside the small avatar) | `PostSlotContext` | Block, small: one or two short lines at small text size (an icon and a few words). |
| `post.footer` | Thread view, post body, after the attached image and before the signature | `PostSlotContext` | Block, full width. For a short strip, not a panel. |
| `profile.sidebar` | Profile page, inside the member's details card, directly under the avatar and above the role/rank list | `UserSlotContext` | Block, centred, narrow. A line or two, not a card. |
| `home.sidebar` | Home page, left column, directly under the Quick Links card | `PageSlotContext` | Block, narrow. Each contribution renders its own card. |
| `layout.head` | `<head>` of the main layout, after core's stylesheets, on every page using it | `PageSlotContext` | No wrapper. `<link rel="stylesheet">` tags only. |

The four portal slots (`home.left`, `home.center.top`, `home.center`,
`home.right`) were added afterwards; see 7.1.

Changed from the proposal: `post.author` was described as inline, and
`profile.sidebar` as a card after the details card. Both moved to directly
under the member's name/avatar, which is where a per-member marker reads
naturally, and the tag helper writes no inline variant.

**Profile settings.** There's no `profile.settings` slot for adding a
section to the member's own Edit Profile form: that would mean a mod's form
fields inside a core form (or a form nested next to it), which section 10
rules out for now. A mod with per-member settings shows a link from
`profile.sidebar` when `IsViewerOwnProfile` is true, pointing at its own
`[Mod]` page with its own antiforgery-protected form. The clans mod does
this.

### 7.1 Portal home page slots

The home page is a three-column portal: a narrow left column (Quick Links),
a wide centre column (Latest Discussions) and a narrow right column
(Statistics, Newest Members). A mod can add a widget to any of them:

| Slot | Page and position | Context | Layout |
| --- | --- | --- | --- |
| `home.left` | Home page, left column, under Quick Links and any `home.sidebar` contributions | `PageSlotContext` | Block, narrow (2 of 12 grid columns on desktop, full width on phones). Each contribution renders its own card. |
| `home.center.top` | Home page, centre column, above Latest Discussions | `PageSlotContext` | Block, wide (8 of 12). For something meant to be seen first. Each contribution renders its own card. |
| `home.center` | Home page, centre column, under Latest Discussions | `PageSlotContext` | Block, wide (8 of 12). The main place for a larger widget. Each contribution renders its own card. |
| `home.right` | Home page, right column, under Statistics and Newest Members | `PageSlotContext` | Block, narrow, like `home.left`. Each contribution renders its own card. |

**`home.sidebar` is kept, unchanged.** It was the only home page slot in the
first cut and the clans mod uses it, so it still renders exactly where it
always has: in the left column directly under Quick Links. `home.left`
renders straight after it, so the left column reads Quick Links, then
`home.sidebar` contributions, then `home.left` contributions. It's not an
alias that merges the two lists (that would change the wrapper class
existing mods' CSS may target, `mod-slot-home-sidebar`); new mods should
pick `home.left` (or another column) instead.

**No empty gaps.** Every column already holds core cards, so all three
columns always render, and an empty slot writes no markup at all (5.2 step
5), so a site with no home page mods looks exactly as before. Each
contribution should render a Bootstrap card with `mb-4`, matching the core
cards around it; the Newest Members card now carries `mb-4` too, so a
`home.right` card below it doesn't butt up against it.

Natural next candidates: `profile.main` (right column, for public profile
sections), `members.row` (Members list, per row, uses the same batching),
`thread.header` (above the first post), `layout.body-end` (for a mod's
script tags), and `message.author` (private message view). Admin pages get
no slots for now, see 10.

## 8. When a mod is disabled, incompatible or missing

| State | What renders | Restart needed |
| --- | --- | --- |
| Enabled | Its contributions | No |
| Disabled by an admin | Nothing. The view component isn't even invoked, so the mod makes no API calls. | No. Takes effect within the 10s enabled-set cache. Immediate on the App instance where the admin clicked, because `SetEnabledAsync` clears its cache. |
| Incompatible (outside its core version range) | Nothing, because an incompatible mod can't be enabled. | No |
| Enabled in the registry, App DLL missing | Nothing. There's no `IAppMod` to contribute. | No |
| App DLL present, API side disabled or failing | The view component's API call fails, the tag helper catches it and renders nothing for that mod. Logged. | No |
| App DLL present, enabled, new version dropped in | New contributions apply after an App restart, the same as for nav items and pages today. | Yes, App only |

Disabling never deletes the mod's data. Re-enabling brings the slots back
exactly as they were.

This uses the same enabled set as nav items and `ModEnabledFilter`, so
pages, nav links and slots for a mod always switch together.

## 9. Worked example: a badges mod

Built the same way as MvcBB.Gallery: its own solution, two projects, both
referencing only ModKit and Shared.

**Badges.Api** (`IApiMod`, Id `"badges"`)
- Tables: `Badges` (id, name, icon class or image, description) and
  `UserBadges` (user id, badge id, awarded at, awarded by).
- `GET api/badges/users?ids=...`: bulk lookup, returns
  `{ userId: [badge...] }`, capped at 200 ids per call. `[AllowAnonymous]`,
  because posts and profiles are visible to guests and badges are public by
  design.
- Award and revoke endpoints: Administrator or Moderator only, checked in
  the API.

**Badges.App** (`IAppMod`, Id `"badges"`)

```csharp
public IEnumerable<ModSlotContribution> GetSlotContributions()
{
    yield return new(CoreSlots.PostAuthor, typeof(PostAuthorBadgesViewComponent));
    yield return new(CoreSlots.ProfileSidebar, typeof(ProfileBadgesViewComponent));
}

public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddHttpClient("BadgesApi", c => { /* base address, 3s timeout */ });
    services.AddScoped<BadgeLookup>();   // wraps SlotBatch<string, IReadOnlyList<Badge>>
}
```

```csharp
public class PostAuthorBadgesViewComponent(BadgeLookup lookup) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(PostSlotContext context)
    {
        var badges = await lookup.ForUserAsync(context.AuthorUserId, context.Page.UserIds);
        if (badges is not { Count: > 0 })
            return Content(string.Empty);
        return View("~/Views/Badges/Slots/PostAuthor.cshtml", badges);
    }
}
```

On a 300-post thread by 12 different people, this is one call to
`api/badges/users` with 12 ids, then 299 in-memory lookups.

An admin page for awarding badges is an ordinary `[Mod("badges")]`
controller with a nav link, exactly as Gallery does it today.

Gallery could also adopt slots as a second reference, for example a
"Recent uploads" card in `profile.sidebar`, but nothing in Gallery needs to
change for this design.

## 10. Security

**Trust model doesn't change.** A mod is .NET code running in-process with
full trust, and already is today. Slots don't sandbox it and don't pretend
to. Admins should install only mods they trust, and the ModKit docs should
say so plainly. What the design does is avoid making the *accidental* mistakes
easy.

- **Minimal context.** Contexts carry ids and display names only, never
  emails, tokens, deleted-post content or moderator-only fields. A mod can
  still reach `HttpContext`, but it has to go out of its way to do so.
- **Only for content the viewer can already see.** Slot contexts are built
  from data the core page has already authorised (board visibility, deleted
  posts, etc.), so ids passed to a mod are never ones the viewer couldn't
  see. Core should not render post slots for posts it hides from the viewer.
- **Mod APIs must check for themselves.** A mod's bulk endpoint receives
  ids from the App, but anyone can call the API directly with any ids. If a
  mod's data isn't public, it must enforce access in its API, as Gallery
  does. Keying lookups by user rather than by post avoids most board-
  visibility questions.
- **Anonymous viewers.** Thread and profile pages can be seen by guests, who
  have no `api_token` claim. Gallery's pattern (throw if no token) would
  fail on every guest page view. A slot view component must handle "no
  token": either call an anonymous read endpoint, or render nothing for
  guests. The badges example uses an anonymous read endpoint. Every context
  carries `ViewerUserId` (null for guests), so a mod can also choose to show
  nothing to guests; the clans mod does that by default, with a setting to
  show clans to guests too.
- **Output encoding.** View component output goes through Razor, so it's
  encoded by default. Mods must not `Html.Raw` user-supplied text. For
  BBCode, use core's `IBBCodeService`, which encodes first, as Gallery does.
- **No forms in display slots.** All the slots are display-only. If
  a mod needs an action (e.g. "award badge" from a profile), it should link
  to its own `[Mod]` controller page, which carries its own `[Authorize]`
  and antiforgery token, rather than post a form from inside a core page.
- **No admin-page slots yet.** Keeping mods out of admin views for now
  avoids mods adding controls next to core admin actions, where a
  confusing or malicious button does the most harm.
- **CSP.** The current policy allows `'self'` and inline scripts and styles,
  so slot markup works without changes. Mods should still prefer Bootstrap
  classes and icons over inline scripts.
- **Failure isolation.** A mod that throws renders nothing and is logged.
  It can't take down a core page, and it doesn't reveal exception details to
  visitors.
- **Name collisions.** Invoking by type avoids view component name clashes.
  Views are a separate risk: precompiled views from two assemblies at the
  same path (e.g. `Views/Shared/Components/Badges/Default.cshtml`) collide
  and one silently wins. Mods should use explicit view paths under their own
  folder, e.g. `~/Views/{ModName}/...`, as in the example.

## 11. Implementation (as built)

ModKit (version 1.0.0 to 1.1.0, in `MvcBB.ModKit.csproj`):
- `CoreSlots.cs`, `ModSlotContribution.cs`, `SlotContexts.cs`
  (`SlotPage`, `SlotContext`, `PageSlotContext`, `UserSlotContext`,
  `PostSlotContext`), `SlotBatch.cs`
- `IAppMod.cs`: `GetSlotContributions()` with a default body

MvcBB.App:
- `Mods/ModSlotRegistry.cs`: known slots (name, context type, wrapper),
  built and validated at startup
- `TagHelpers/ModSlotTagHelper.cs`: section 5.2
- `Program.cs`: registers the registry and resolves it after `Build()`
- `Views/_ViewImports.cshtml`: `@using MvcBB.ModKit`,
  `@addTagHelper *, MvcBB.App`
- `Views/Threads/Details.cshtml`: builds `SlotPage`, adds `post.author` and
  `post.footer`
- `Views/Account/Profile.cshtml`: adds `profile.sidebar`
- `Views/Home/Index.cshtml`: adds `home.sidebar`, then (7.1) `home.left`,
  `home.center.top`, `home.center` and `home.right`
- `Views/Shared/_Layout.cshtml`: adds `layout.head`
- `README.md`: Mods section mentions slots and mod static files

MvcBB.Gallery: no changes. Builds and runs unchanged against ModKit 1.1.0.

The first mod using slots is MvcBB.Clans (its own solution), which uses
`post.author`, `profile.sidebar`, `home.sidebar` and `layout.head`, the
batching helper, and the `/_mods/{modId}/` static file route for its CSS.

Checked by hand against the in-memory provider: with no slot-using mods the
pages render no slot markup; with the clans mod enabled, a thread page makes
one membership lookup for all its authors; disabling the mod removes its
slot output, stylesheet, pages and icons immediately on the instance where
it was disabled; guests see nothing from it unless the mod allows them.
There's still no automated test project.

## 12. Mod static files (implemented)

Status: **implemented** in MvcBB.App. Independent of the slot
work above, and useful to any mod with its own CSS, scripts or images.

**Why core needs it.** A Razor Class Library's usual `wwwroot` handling
(`/_content/{Assembly}/...`) is set up by the host's build, so it never
applies to a mod DLL dropped into `Mods/` at runtime.

**What core does.** At startup, for each discovered `IAppMod`, MvcBB.App
looks for an embedded files manifest in that mod's assembly. If it finds
one, it serves the mod's embedded `wwwroot` folder at
`/_mods/{modId}/{path}`. Code: `MvcBB.App/Extensions/ModStaticFilesExtensions.cs`,
called from `Program.cs` straight after `app.UseStaticFiles()`.

- **Prefix `/_mods`, not `/mods`.** Routing matches case-insensitively, so
  `/mods/...` would share a URL space with the admin Mods page
  (`/Mods/Index`, `/Mods/SetEnabled`).
- **Disabled mods' files 404**, using the same cached enabled set as the
  mod's pages and nav links. If the API can't be reached, the files are
  treated as disabled and a warning is logged.
- **No manifest, no route.** A mod that ships no static files (Gallery
  today) is skipped silently.
- Mods whose Id isn't URL-safe (`A-Z a-z 0-9 _ -`) or duplicates another
  mod's Id are skipped with a warning.
- Files go through the same pipeline position as core's own static files,
  so they get the security headers (`nosniff`, CSP) and skip auth and
  routing. They're public: don't embed anything that should be login-only.
  Login-only files should come from a `[Mod]` controller, as Gallery's
  images do.

**What a mod writer does.** In the mod's App-side `.csproj`:

```xml
<PropertyGroup>
  <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="10.0.0" />
  <EmbeddedResource Include="wwwroot\**" />
</ItemGroup>
```

Then put files under the project's `wwwroot/` and link them as
`/_mods/{modId}/css/badges.css` (in Razor, `~/_mods/badges/css/badges.css`).
The package reference is only needed for the build step that writes the
manifest. Changing a file means rebuilding the DLL and restarting MvcBB.App,
as for any other mod change.

## 13. Open questions

1. **Admin control over order or placement.** `Order` is chosen by the mod.
   If two mods fight over the top spot, an admin override (per slot, per mod)
   could come later.
2. **Per-slot guest visibility.** Should core offer a flag on the
   contribution ("members only") so the tag helper skips it for guests, or
   leave that to each mod? Leaving it to the mod is simpler for now.
