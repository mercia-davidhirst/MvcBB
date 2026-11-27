/*
    Upgrade: MvcBB v1.4.0 -> v1.4.1 (SQL Server)

    Adds "Remember me" sign-in and the settings for the home page "Who's
    online" card. Safe to run any number of times: every change is guarded,
    so a database that already has it (including a fresh v1.4.1 install,
    whose Tables/ scripts create the new shape directly) is left as it is.
    No existing rows are changed or deleted.

    install.ps1 runs this straight after Tables/ and then recreates every
    stored procedure, including the new usp_RefreshToken_* ones, so
    re-running install.ps1 is the normal way to upgrade. To run it on its
    own (you will still need the usp_RefreshToken_*.sql procedures):
        sqlcmd -S <server> -d <database> -E -b -I -i Upgrades/v1.4.1.sql

    1. dbo.RefreshTokens - one row per "Remember me" refresh token (stored
       only as a SHA-256 hash). The same definition as
       Tables/30_RefreshTokens.sql.
    2. dbo.DisplaySettings.ShowWhosOnline (default on) and
       OnlineWindowMinutes (default 15) - set on Admin > Settings.
*/

SET NOCOUNT ON;
GO

-- 1. RefreshTokens table.
IF OBJECT_ID('dbo.RefreshTokens') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        UserId          INT                NOT NULL,
        TokenHash       CHAR(64)           NOT NULL,
        FamilyId        UNIQUEIDENTIFIER   NOT NULL,
        SessionStamp    VARCHAR(64)        NOT NULL,
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ExpiresAt       DATETIME2(3)       NOT NULL,
        UsedAt          DATETIME2(3)       NULL,
        RevokedAt       DATETIME2(3)       NULL,
        CreatedIp       VARCHAR(45)        NULL,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash)
    );

    CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_FamilyId ON dbo.RefreshTokens (FamilyId);
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_ExpiresAt ON dbo.RefreshTokens (ExpiresAt);
END
GO

-- 2. DisplaySettings.ShowWhosOnline / OnlineWindowMinutes - the home page
--    "Who's online" card (on by default) and how many minutes count as
--    online (default 15).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'ShowWhosOnline')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD ShowWhosOnline BIT NOT NULL CONSTRAINT DF_DisplaySettings_ShowWhosOnline DEFAULT (1);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'OnlineWindowMinutes')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD OnlineWindowMinutes INT NOT NULL CONSTRAINT DF_DisplaySettings_OnlineWindowMinutes DEFAULT (15);
END
GO
