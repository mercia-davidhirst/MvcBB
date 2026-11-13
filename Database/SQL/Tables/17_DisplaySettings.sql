/*
    Table:   dbo.DisplaySettings
    Purpose: Backing store for MvcBB.Shared.Models.Settings.DisplaySettings /
             IDisplaySettingsRepository. A single configuration row (Id is
             always 1, enforced by the CHECK constraint) - there's exactly
             one row, seeded once by Seed Data/17_DisplaySettings.sql, and
             only ever UPDATEd (see usp_DisplaySettings_Update), never
             inserted or deleted at runtime.
*/

DROP TABLE IF EXISTS dbo.DisplaySettings;
GO

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
    CONSTRAINT PK_DisplaySettings PRIMARY KEY CLUSTERED (Id)
);
GO
