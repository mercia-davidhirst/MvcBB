/*
    Table:   dbo.Mods
    Purpose: Backing store for MvcBB.Shared.Models.Mod.ModRecord / IModRepository -
             the mod framework's registry of discovered third-party mod
             assemblies (see MvcBB.ModKit) and their admin-controlled
             enabled/disabled state.
    Notes:
      - Id is the mod's own stable key (e.g. "gallery"), not an identity
        column - it's declared by the mod itself and must match between its
        API-side (IApiMod) and App-side (IAppMod) assemblies.
      - IsEnabled defaults to 0 and is only ever flipped by an admin via
        usp_Mod_SetEnabled - rediscovering a mod at startup (usp_Mod_Upsert)
        never changes it, so a toggle survives app restarts/redeploys.
      - InstalledVersion tracks the last Version for which IApiMod.InstallAsync
        completed successfully, so it only reruns on an actual version bump,
        not on every startup. It's set via usp_Mod_SetInstalledVersion, never
        by usp_Mod_Upsert, since discovery and install are separate steps.
*/

IF OBJECT_ID('dbo.Mods') IS NULL
BEGIN
    CREATE TABLE dbo.Mods
    (
        Id                  NVARCHAR(100)      NOT NULL,
        Name                NVARCHAR(200)      NOT NULL,
        Version             NVARCHAR(50)       NOT NULL,
        MinCoreVersion      NVARCHAR(50)       NOT NULL,
        MaxCoreVersion      NVARCHAR(50)       NULL,
        InstalledVersion    NVARCHAR(50)       NULL,
        IsEnabled           BIT                NOT NULL CONSTRAINT DF_Mods_IsEnabled DEFAULT (0),
        Status              NVARCHAR(20)       NOT NULL CONSTRAINT DF_Mods_Status DEFAULT ('Compatible'),
        DiscoveredAt        DATETIME2(3)       NOT NULL CONSTRAINT DF_Mods_DiscoveredAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Mods PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Mods') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Mods ADD NewColumn ...;
--   END
--   GO
