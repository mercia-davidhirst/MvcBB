/*
    Table:   dbo.Threads
    Purpose: Backing store for MvcBB.Shared.Models.ForumThread.ForumThread /
             IForumThreadRepository.
    Notes:
      - ViewCount, PostCount, LastPostAt and LastPostByUserId are maintained by
        the application layer, matching ThreadsController/PostsController.
      - IX_Threads_BoardId_Listing supports IForumThreadRepository.GetByBoardId,
        which orders by IsSticky desc, then (LastPostAt ?? CreatedAt) desc.
*/

IF OBJECT_ID('dbo.Threads') IS NULL
BEGIN
    CREATE TABLE dbo.Threads
    (
        Id                  INT IDENTITY(1,1)  NOT NULL,
        Title               NVARCHAR(200)      NOT NULL,
        BoardId             INT                NOT NULL,
        CreatedByUserId     INT                NOT NULL,
        CreatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_Threads_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(3)       NULL,
        IsSticky            BIT                NOT NULL CONSTRAINT DF_Threads_IsSticky DEFAULT (0),
        IsLocked            BIT                NOT NULL CONSTRAINT DF_Threads_IsLocked DEFAULT (0),
        ViewCount           INT                NOT NULL CONSTRAINT DF_Threads_ViewCount DEFAULT (0),
        PostCount           INT                NOT NULL CONSTRAINT DF_Threads_PostCount DEFAULT (0),
        TotalLikes          INT                NOT NULL CONSTRAINT DF_Threads_TotalLikes DEFAULT (0),
        ParticipantCount    INT                NOT NULL CONSTRAINT DF_Threads_ParticipantCount DEFAULT (0),
        LastPostAt          DATETIME2(3)       NULL,
        LastPostByUserId    INT                NULL,
        IsImportedData      BIT                NOT NULL CONSTRAINT DF_Threads_IsImportedData DEFAULT (0),
        CONSTRAINT PK_Threads PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Threads_Board FOREIGN KEY (BoardId) REFERENCES dbo.Boards (Id),
        CONSTRAINT FK_Threads_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Threads_LastPostByUser FOREIGN KEY (LastPostByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Threads_BoardId_Listing ON dbo.Threads (BoardId, IsSticky DESC, LastPostAt DESC, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_Threads_CreatedByUserId ON dbo.Threads (CreatedByUserId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Threads') AND name = 'TotalLikes')
BEGIN
    ALTER TABLE dbo.Threads ADD TotalLikes INT NOT NULL CONSTRAINT DF_Threads_TotalLikes DEFAULT (0);

    -- One-time backfill from the data this column replaces - matches what
    -- ThreadsController.MapToThreadResponses used to compute on the fly
    -- (likes across every post in the thread). Run via EXEC() so the
    -- reference to the column just added above is resolved at execution
    -- time, not batch-parse time (SQL Server validates column names for the
    -- whole batch up front, before the ALTER above has actually run).
    EXEC(N'
        UPDATE t
        SET t.TotalLikes = (
            SELECT COUNT(*)
            FROM dbo.Posts AS p
            INNER JOIN dbo.PostLikes AS pl ON pl.PostId = p.Id
            WHERE p.ThreadId = t.Id
        )
        FROM dbo.Threads AS t;
    ');
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Threads') AND name = 'IsImportedData')
BEGIN
    ALTER TABLE dbo.Threads ADD IsImportedData BIT NOT NULL CONSTRAINT DF_Threads_IsImportedData DEFAULT (0);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Threads') AND name = 'ParticipantCount')
BEGIN
    ALTER TABLE dbo.Threads ADD ParticipantCount INT NOT NULL CONSTRAINT DF_Threads_ParticipantCount DEFAULT (0);

    -- One-time backfill - matches what ThreadsController.MapToThreadResponses
    -- used to compute on the fly (distinct authors across the thread's
    -- posts). Run via EXEC() - see the TotalLikes block above for why.
    EXEC(N'
        UPDATE t
        SET t.ParticipantCount = (
            SELECT COUNT(DISTINCT p.CreatedByUserId)
            FROM dbo.Posts AS p
            WHERE p.ThreadId = t.Id
        )
        FROM dbo.Threads AS t;
    ');
END
GO
