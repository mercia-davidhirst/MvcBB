/*
    Table:   dbo.PollOptions
    Purpose: Backing store for MvcBB.Shared.Models.Poll.PollOption /
             IPollRepository.
*/

IF OBJECT_ID('dbo.PollOptions') IS NULL
BEGIN
    CREATE TABLE dbo.PollOptions
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        PollId          INT                NOT NULL,
        Text            NVARCHAR(200)      NOT NULL,
        SortOrder       INT                NOT NULL CONSTRAINT DF_PollOptions_SortOrder DEFAULT (0),
        CONSTRAINT PK_PollOptions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PollOptions_Poll FOREIGN KEY (PollId) REFERENCES dbo.Polls (Id)
    );

    CREATE NONCLUSTERED INDEX IX_PollOptions_PollId ON dbo.PollOptions (PollId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PollOptions') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.PollOptions ADD NewColumn ...;
--   END
--   GO
