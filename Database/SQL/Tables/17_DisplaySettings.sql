/*
    Table:   dbo.DisplaySettings
    Purpose: Backing store for MvcBB.Shared.Models.Settings.DisplaySettings /
             IDisplaySettingsRepository. A single configuration row (Id is
             always 1, enforced by the CHECK constraint) - there's exactly
             one row, seeded once by Seed Data/17_DisplaySettings.sql, and
             only ever UPDATEd (see usp_DisplaySettings_Update), never
             inserted or deleted at runtime.
*/

IF OBJECT_ID('dbo.DisplaySettings') IS NULL
BEGIN
    CREATE TABLE dbo.DisplaySettings
    (
        Id                  INT            NOT NULL CONSTRAINT CK_DisplaySettings_SingletonId CHECK (Id = 1),
        ThreadsPerPage      INT            NOT NULL,
        SiteTitle           NVARCHAR(100)  NOT NULL,
        WelcomeMessage      NVARCHAR(500)  NOT NULL,
        SiteName            NVARCHAR(50)   NOT NULL,
        LogoPath            NVARCHAR(500)  NULL,
        FooterDisclaimer    NVARCHAR(500)  NULL,
        RegistrationEnabled BIT            NOT NULL,
        DateFormat          NVARCHAR(20)   NOT NULL CONSTRAINT DF_DisplaySettings_DateFormat DEFAULT ('dd/MM/yyyy'),
        TimeFormat          NVARCHAR(20)   NOT NULL CONSTRAINT DF_DisplaySettings_TimeFormat DEFAULT ('HH:mm'),
        CONSTRAINT PK_DisplaySettings PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'DateFormat')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD DateFormat NVARCHAR(20) NOT NULL CONSTRAINT DF_DisplaySettings_DateFormat DEFAULT ('dd/MM/yyyy');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'TimeFormat')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD TimeFormat NVARCHAR(20) NOT NULL CONSTRAINT DF_DisplaySettings_TimeFormat DEFAULT ('HH:mm');
END
GO
