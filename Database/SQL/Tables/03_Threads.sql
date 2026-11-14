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
        LastPostAt          DATETIME2(3)       NULL,
        LastPostByUserId    INT                NULL,
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
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Threads') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Threads ADD NewColumn ...;
--   END
--   GO
