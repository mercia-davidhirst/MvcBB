/*
    Table:   dbo.PostLikes
    Purpose: Backing store for MvcBB.Shared.Interfaces.IPostLikeRepository
             (HasLiked, GetLikeCount, GetLikeCounts, GetLikedPostIds, AddLike,
             RemoveLike). This table did not exist in this folder before -
             SqlPostLikeRepository/PostgreSqlPostLikeRepository already called
             usp_PostLike_*/fn_post_like_* procedures against it, but nothing
             had actually created them, so every call would have failed
             against a database deployed only from this folder.
    Notes:
      - Composite primary key (PostId, UserId): a user can like a given post
        at most once, matching HasLiked/AddLike's semantics (AddLike is a
        no-op if the like already exists - see the repositories).
*/

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.PostLikes');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.PostLikes;
GO

CREATE TABLE dbo.PostLikes
(
    PostId      INT             NOT NULL,
    UserId      INT             NOT NULL,
    CreatedAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_PostLikes_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_PostLikes PRIMARY KEY CLUSTERED (PostId, UserId),
    CONSTRAINT FK_PostLikes_Posts FOREIGN KEY (PostId) REFERENCES dbo.Posts (Id),
    CONSTRAINT FK_PostLikes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
);
GO

CREATE NONCLUSTERED INDEX IX_PostLikes_PostId ON dbo.PostLikes (PostId);
GO
