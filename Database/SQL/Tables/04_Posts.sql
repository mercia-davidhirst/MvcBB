/*
    Table:   dbo.Posts
    Purpose: Backing store for MvcBB.Shared.Models.Post.Post / IPostRepository.
    Notes:
      - Content is NVARCHAR(MAX) because the model's 10000-character limit
        exceeds the 4000-character cap of a plain NVARCHAR(n); the limit is
        enforced with a CHECK constraint instead.
      - ThreadTitle, CreatedByUsername, UpdatedByUsername, DeletedByUsername,
        UserAvatar, UserSignature, UserPostCount, UserJoinedAt and UserRole on
        the Post model are all join/aggregate-derived presentation fields, not
        stored columns here - see dbo.vw_PostDetails, which reconstructs them.
      - QuotedPostId is a self-reference; ON DELETE/UPDATE is left as NO ACTION
        (the default) to avoid multiple cascade paths back into dbo.Posts.
*/

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Posts');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Posts;
GO

CREATE TABLE dbo.Posts
(
    Id                  INT IDENTITY(1,1)  NOT NULL,
    Content             NVARCHAR(MAX)      NOT NULL,
    ThreadId            INT                NOT NULL,
    CreatedByUserId     INT                NOT NULL,
    CreatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_Posts_CreatedAt DEFAULT (SYSUTCDATETIME()),
    IsEdited             BIT               NOT NULL CONSTRAINT DF_Posts_IsEdited DEFAULT (0),
    EditReason          NVARCHAR(500)      NULL,
    UpdatedAt           DATETIME2(3)       NULL,
    UpdatedByUserId     INT                NULL,
    IsDeleted           BIT                NOT NULL CONSTRAINT DF_Posts_IsDeleted DEFAULT (0),
    DeletedByUserId     INT                NULL,
    DeletedAt           DATETIME2(3)       NULL,
    DeleteReason        NVARCHAR(500)      NULL,
    QuotedPostId        INT                NULL,
    CONSTRAINT PK_Posts PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Posts_Thread FOREIGN KEY (ThreadId) REFERENCES dbo.Threads (Id),
    CONSTRAINT FK_Posts_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT FK_Posts_UpdatedByUser FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT FK_Posts_DeletedByUser FOREIGN KEY (DeletedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT FK_Posts_QuotedPost FOREIGN KEY (QuotedPostId) REFERENCES dbo.Posts (Id),
    CONSTRAINT CK_Posts_ContentLength CHECK (LEN(Content) <= 10000)
);
GO

CREATE NONCLUSTERED INDEX IX_Posts_ThreadId_CreatedAt ON dbo.Posts (ThreadId, CreatedAt);
GO

CREATE NONCLUSTERED INDEX IX_Posts_CreatedByUserId ON dbo.Posts (CreatedByUserId) INCLUDE (IsDeleted);
GO
