/*
    Table:   public.mods
    Purpose: Backing store for MvcBB.Shared.Models.Mod.ModRecord / IModRepository -
             the mod framework's registry of discovered third-party mod
             assemblies (see MvcBB.ModKit) and their admin-controlled
             enabled/disabled state.
    Notes:
      - id is the mod's own stable key (e.g. "gallery"), not a generated
        identity column - it's declared by the mod itself and must match
        between its API-side (IApiMod) and App-side (IAppMod) assemblies.
      - is_enabled defaults to FALSE and is only ever flipped by an admin via
        sp_mod_set_enabled - rediscovering a mod at startup (sp_mod_upsert)
        never changes it, so a toggle survives app restarts/redeploys.
      - installed_version tracks the last version for which IApiMod.InstallAsync
        completed successfully, so it only reruns on an actual version bump,
        not on every startup. It's set via sp_mod_set_installed_version, never
        by sp_mod_upsert, since discovery and install are separate steps.
*/

CREATE TABLE IF NOT EXISTS public.mods
(
    id                  VARCHAR(100)    NOT NULL PRIMARY KEY,
    name                VARCHAR(200)    NOT NULL,
    version             VARCHAR(50)     NOT NULL,
    min_core_version    VARCHAR(50)     NOT NULL,
    max_core_version    VARCHAR(50)     NULL,
    installed_version   VARCHAR(50)     NULL,
    is_enabled          BOOLEAN         NOT NULL DEFAULT FALSE,
    status              VARCHAR(20)     NOT NULL DEFAULT 'Compatible',
    discovered_at       TIMESTAMP(3)    NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc')
);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.mods ADD COLUMN IF NOT EXISTS new_column ...;
