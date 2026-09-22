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

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Threads');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Threads;
GO

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
GO

CREATE NONCLUSTERED INDEX IX_Threads_BoardId_Listing ON dbo.Threads (BoardId, IsSticky DESC, LastPostAt DESC, CreatedAt DESC);
GO

CREATE NONCLUSTERED INDEX IX_Threads_CreatedByUserId ON dbo.Threads (CreatedByUserId);
GO
