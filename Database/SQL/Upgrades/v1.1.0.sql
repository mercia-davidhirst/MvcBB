/*
    Upgrade: MvcBB v1.0.0 -> v1.1.0 (SQL Server)

    Schema changes only. Safe to run any number of times: every change is
    guarded, so a database that already has it (including a fresh v1.1.0
    install, whose Tables/ scripts create the new shape directly) is left as
    it is. No existing rows are changed or deleted.

    install.ps1 runs this straight after Tables/ and before the Functions,
    Views and Stored Procedures that use the new columns, so re-running
    install.ps1 is the normal way to upgrade. To run it on its own:
        sqlcmd -S <server> -d <database> -E -b -I -i Upgrades/v1.1.0.sql

    1. dbo.Reports.PageUrl            - page attached to a "Report an issue" report
    2. dbo.Reports CK_Reports_Type    - allows Type 3 (User) and 4 (SiteIssue)
    3. dbo.DisplaySettings.TimeZoneId - forum display time zone ('' = server local time)
*/

SET NOCOUNT ON;
GO

-- 1. Reports.PageUrl
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Reports') AND name = 'PageUrl')
BEGIN
    ALTER TABLE dbo.Reports ADD PageUrl NVARCHAR(500) NULL;
END
GO

-- 2. CK_Reports_Type: v1.0.0 only allowed 0-2, which also rejected User (3)
-- reports. Recreated only when the current definition doesn't allow 4.
IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.Reports')
      AND name = 'CK_Reports_Type'
      AND definition NOT LIKE '%4%'
)
BEGIN
    ALTER TABLE dbo.Reports DROP CONSTRAINT CK_Reports_Type;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.Reports')
      AND name = 'CK_Reports_Type'
)
BEGIN
    ALTER TABLE dbo.Reports WITH CHECK ADD CONSTRAINT CK_Reports_Type CHECK (Type IN (0, 1, 2, 3, 4));
END
GO

-- 3. DisplaySettings.TimeZoneId. Existing rows get '' (server local time),
-- which is how dates were shown before v1.1.0.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'TimeZoneId')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD TimeZoneId NVARCHAR(100) NOT NULL CONSTRAINT DF_DisplaySettings_TimeZoneId DEFAULT (N'');
END
GO
