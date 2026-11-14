/*
    Table:   dbo.Polls
    Purpose: Backing store for MvcBB.Shared.Models.Poll.Poll / IPollRepository.
    Notes:
      - One poll per thread (UNIQUE on ThreadId) - polls are attached only at
        thread-creation time and can't be added retroactively.
*/

IF OBJECT_ID('dbo.Polls') IS NULL
BEGIN
    CREATE TABLE dbo.Polls
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        ThreadId        INT                NOT NULL,
        Question        NVARCHAR(200)      NOT NULL,
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_Polls_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Polls PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Polls_ThreadId UNIQUE (ThreadId),
        CONSTRAINT FK_Polls_Thread FOREIGN KEY (ThreadId) REFERENCES dbo.Threads (Id)
    );
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Polls') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Polls ADD NewColumn ...;
--   END
--   GO
