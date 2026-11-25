/*
    Upgrade: MvcBB v1.3.0 -> v1.3.1 (SQL Server)

    Schema changes only. Safe to run any number of times: every change is
    guarded, so a database that already has it (including a fresh v1.3.1
    install, whose Tables/ scripts create the new shape directly) is left as
    it is. No existing rows are changed or deleted.

    The new v1.3.1 table (ThreadReads) is created by its Tables/ script;
    this only changes tables that existed before.

    install.ps1 runs this straight after Tables/ and before the Functions,
    Views and Stored Procedures that use the new columns, so re-running
    install.ps1 is the normal way to upgrade. To run it on its own:
        sqlcmd -S <server> -d <database> -E -b -I -i Upgrades/v1.3.1.sql

    1. dbo.Users.AutoJumpToUnread - open discussions at the member's first
       unread post (a profile setting, on by default)
*/

SET NOCOUNT ON;
GO

-- 1. Users.AutoJumpToUnread. Existing members get 1 (on).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'AutoJumpToUnread')
BEGIN
    ALTER TABLE dbo.Users ADD AutoJumpToUnread BIT NOT NULL CONSTRAINT DF_Users_AutoJumpToUnread DEFAULT (1);
END
GO
