/*
    Table:   dbo.ModerationQueue
    Purpose: Backing store for MvcBB.Shared.Models.Moderation.ModerationQueueItem /
             IModerationQueueRepository - new members' posts held for a
             moderator's approval (see dbo.ModerationSettings). Added in v1.4.0.
    Notes:
      - A held post isn't a thread or post yet, so nothing else in the forum
        (counts, search, last post) ever sees it. Approving it creates the
        thread or reply then; either way the row is deleted afterwards, and
        dbo.ModerationLog keeps the record of the decision.
      - Kind mirrors QueuedPostKind: 1 = NewThread (Title, BoardId and an
        optional PollJson), 2 = Reply (ThreadId, optional QuotedPostId).
      - BoardId and ThreadId have no foreign keys: if the board or thread
        goes before a moderator gets to it, approving fails with a message
        and the moderator can turn it down instead.
      - Rows go with their member (ON DELETE CASCADE).
*/

IF OBJECT_ID('dbo.ModerationQueue') IS NULL
BEGIN
    CREATE TABLE dbo.ModerationQueue
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        Kind            TINYINT            NOT NULL,
        UserId          INT                NOT NULL,
        BoardId         INT                NOT NULL,
        ThreadId        INT                NULL,
        Title           NVARCHAR(200)      NULL,
        Content         NVARCHAR(MAX)      NOT NULL,
        QuotedPostId    INT                NULL,
        ImageUrl        NVARCHAR(500)      NULL,
        PollJson        NVARCHAR(MAX)      NULL,
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_ModerationQueue_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ModerationQueue PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ModerationQueue_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT CK_ModerationQueue_Kind CHECK ((Kind = 1 AND Title IS NOT NULL) OR (Kind = 2 AND ThreadId IS NOT NULL))
    );

    CREATE NONCLUSTERED INDEX IX_ModerationQueue_UserId ON dbo.ModerationQueue (UserId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table: none pending yet - add guarded ALTER TABLE statements here.
