/*
    Upgrade: MvcBB v1.3.1 -> v1.4.0 (SQL Server)

    Schema changes, plus one new BBCode tag row. Safe to run any number of
    times: every change is guarded, so a database that already has it
    (including a fresh v1.4.0 install, whose Tables/ scripts create the new
    shape directly) is left as it is. No existing rows are changed or
    deleted.

    The new v1.4.0 tables are created by their own Tables/ scripts, which
    install.ps1 runs just before this one:
        Tables/26_Notifications.sql       - the notification centre
        Tables/27_ModerationQueue.sql     - posts held for approval
        Tables/28_ModerationLog.sql       - the moderation log
        Tables/29_ModerationSettings.sql  - when to hold new members' posts
                                            (seeded off by Seed Data/
                                            19_ModerationSettings.sql)
    This script only changes what existed before.

    install.ps1 runs this straight after Tables/ and before the Functions,
    Views and Stored Procedures that use the new columns, so re-running
    install.ps1 is the normal way to upgrade. To run it on its own:
        sqlcmd -S <server> -d <database> -E -b -I -i Upgrades/v1.4.0.sql

    1. dbo.Users.BirthDay, BirthMonth, BirthYear - a member's birthday, set
       on their profile. Day and month together, year optional. Existing
       members have none.
    2. dbo.BBCodeTags - adds the Spoiler tag to the reference list on
       Admin > BBCode, for a database whose list was seeded before v1.4.0
       (an empty table is left for Seed Data/07_BBCodeTags.sql, which now
       includes it). The tag itself works either way.
    3. dbo.DisplaySettings.PostsPerPage - how many posts a discussion shows
       per page (default 20; discussions are paged from v1.4.0).
*/

SET NOCOUNT ON;
GO

-- 1. Users birthday columns.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'BirthDay')
BEGIN
    ALTER TABLE dbo.Users ADD BirthDay TINYINT NULL, BirthMonth TINYINT NULL, BirthYear SMALLINT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Users_Birthday')
BEGIN
    ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Birthday CHECK ((BirthDay IS NULL AND BirthMonth IS NULL AND BirthYear IS NULL)
        OR (BirthDay BETWEEN 1 AND 31 AND BirthMonth BETWEEN 1 AND 12 AND (BirthYear IS NULL OR BirthYear BETWEEN 1900 AND 9999)));
END
GO

-- 2. Spoiler in the BBCode tag list (existing, already-seeded lists only).
IF EXISTS (SELECT 1 FROM dbo.BBCodeTags)
   AND NOT EXISTS (SELECT 1 FROM dbo.BBCodeTags WHERE Name = N'Spoiler')
BEGIN
    INSERT INTO dbo.BBCodeTags (Name, Pattern, Replacement, Description, Example, IsActive, SortOrder)
    VALUES (N'Spoiler', N'\[spoiler(=(.*?))?\](.*?)\[/spoiler\]', N'<details class="bbcode-spoiler"><summary>$2</summary>$3</details>',
            N'Hides text until the reader clicks it, with an optional title', N'[spoiler=Ending]hidden text[/spoiler]', 1,
            (SELECT ISNULL(MAX(SortOrder), 0) + 1 FROM dbo.BBCodeTags));
END
GO

-- 3. DisplaySettings.PostsPerPage - posts per page of a discussion (default 20).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DisplaySettings') AND name = 'PostsPerPage')
BEGIN
    ALTER TABLE dbo.DisplaySettings ADD PostsPerPage INT NOT NULL CONSTRAINT DF_DisplaySettings_PostsPerPage DEFAULT (20);
END
GO
