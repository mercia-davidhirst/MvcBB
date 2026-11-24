/*
    Upgrade: MvcBB v1.2.x -> v1.3.0 (SQL Server)

    Schema changes only. Safe to run any number of times: every change is
    guarded, so a database that already has it (including a fresh v1.3.0
    install, whose Tables/ scripts create the new shape directly) is left as
    it is. No existing rows are changed or deleted.

    The new v1.3.0 tables (AdminEmailFailures, AdminEmailDeliveries,
    EmailSendLog) are created by their Tables/ scripts; this only changes
    tables that existed before.

    install.ps1 runs this straight after Tables/ and before the Functions,
    Views and Stored Procedures that use the new columns, so re-running
    install.ps1 is the normal way to upgrade. To run it on its own:
        sqlcmd -S <server> -d <database> -E -b -I -i Upgrades/v1.3.0.sql

    1. dbo.DisplaySettings.MaxEmailsPerHour / MaxEmailsPerDay - host sending
       limits set in Admin > Settings (0 = use the API's appsettings.json)
    2. dbo.AdminEmails.PausedUntil - when a sending email expects to carry on
       after waiting for those limits (the table was added in v1.2.0)
*/

SET NOCOUNT ON;
GO

-- 1. DisplaySettings sending limits. Existing rows get 0 (no limit set here).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'MaxEmailsPerHour')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD MaxEmailsPerHour INT NOT NULL CONSTRAINT DF_DisplaySettings_MaxEmailsPerHour DEFAULT (0);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'MaxEmailsPerDay')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD MaxEmailsPerDay INT NOT NULL CONSTRAINT DF_DisplaySettings_MaxEmailsPerDay DEFAULT (0);
END
GO

-- 2. AdminEmails.PausedUntil
IF OBJECT_ID('dbo.AdminEmails') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AdminEmails') AND name = 'PausedUntil')
BEGIN
    ALTER TABLE dbo.AdminEmails ADD PausedUntil DATETIME2(3) NULL;
END
GO
