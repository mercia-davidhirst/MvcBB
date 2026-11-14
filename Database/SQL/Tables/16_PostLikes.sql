/*
    Table:   dbo.PostLikes
    Purpose: Backing store for MvcBB.Shared.Interfaces.IPostLikeRepository
             (HasLiked, GetLikeCount, GetLikeCounts, GetLikedPostIds, AddLike,
             RemoveLike). This table did not exist in this folder before -
             SqlPostLikeRepository/PostgreSqlPostLikeRepository already called
             usp_PostLike_* / fn_post_like_* procedures against it, but nothing
             had actually created them, so every call would have failed
             against a database deployed only from this folder.
    Notes:
      - Composite primary key (PostId, UserId): a user can like a given post
        at most once, matching HasLiked/AddLike's semantics (AddLike is a
        no-op if the like already exists - see the repositories).
*/

IF OBJECT_ID('dbo.PostLikes') IS NULL
BEGIN
    CREATE TABLE dbo.PostLikes
    (
        PostId      INT             NOT NULL,
        UserId      INT             NOT NULL,
        CreatedAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_PostLikes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_PostLikes PRIMARY KEY CLUSTERED (PostId, UserId),
        CONSTRAINT FK_PostLikes_Posts FOREIGN KEY (PostId) REFERENCES dbo.Posts (Id),
        CONSTRAINT FK_PostLikes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
    );

    CREATE NONCLUSTERED INDEX IX_PostLikes_PostId ON dbo.PostLikes (PostId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PostLikes') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.PostLikes ADD NewColumn ...;
--   END
--   GO
