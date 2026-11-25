/*
    Table:   dbo.ThreadReads
    Purpose: Backing store for
             MvcBB.Shared.Models.ThreadRead.ThreadReadModel /
             IThreadReadRepository - how far each member has read each
             discussion, so it can open at their first unread post.
    Notes:
      - One row per (ThreadId, UserId), created the first time they scroll a
        post into view. Guests aren't tracked.
      - LastReadPostAt is the CreatedAt of the newest post they've seen;
        posts are shown oldest first, so anything newer is unread. It only
        ever moves forward (see usp_ThreadRead_MarkRead).
      - LastReadPostId is kept for reference only and has no foreign key, so
        deleting that post never blocks anything or loses the position.
      - Rows go with their thread or member (ON DELETE CASCADE).
*/

IF OBJECT_ID('dbo.ThreadReads') IS NULL
BEGIN
    CREATE TABLE dbo.ThreadReads
    (
        ThreadId            INT                NOT NULL,
        UserId              INT                NOT NULL,
        LastReadPostId      INT                NOT NULL,
        LastReadPostAt      DATETIME2(3)       NOT NULL,
        UpdatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_ThreadReads_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ThreadReads PRIMARY KEY CLUSTERED (UserId, ThreadId),
        CONSTRAINT FK_ThreadReads_Thread FOREIGN KEY (ThreadId) REFERENCES dbo.Threads (Id) ON DELETE CASCADE,
        CONSTRAINT FK_ThreadReads_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_ThreadReads_ThreadId ON dbo.ThreadReads (ThreadId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ThreadReads') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.ThreadReads ADD NewColumn ...;
--   END
--   GO
