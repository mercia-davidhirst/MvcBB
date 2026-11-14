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
      - ImageUrl is an optional relative path (e.g. "/images/posts/{guid}.png")
        to an attached image, matching Users.AvatarUrl's sizing convention.
*/

IF OBJECT_ID('dbo.Posts') IS NULL
BEGIN
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
        ImageUrl            NVARCHAR(500)      NULL,
        CONSTRAINT PK_Posts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Posts_Thread FOREIGN KEY (ThreadId) REFERENCES dbo.Threads (Id),
        CONSTRAINT FK_Posts_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Posts_UpdatedByUser FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Posts_DeletedByUser FOREIGN KEY (DeletedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Posts_QuotedPost FOREIGN KEY (QuotedPostId) REFERENCES dbo.Posts (Id),
        CONSTRAINT CK_Posts_ContentLength CHECK (LEN(Content) <= 10000)
    );

    CREATE NONCLUSTERED INDEX IX_Posts_ThreadId_CreatedAt ON dbo.Posts (ThreadId, CreatedAt);
    CREATE NONCLUSTERED INDEX IX_Posts_CreatedByUserId ON dbo.Posts (CreatedByUserId) INCLUDE (IsDeleted);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Posts') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Posts ADD NewColumn ...;
--   END
--   GO
