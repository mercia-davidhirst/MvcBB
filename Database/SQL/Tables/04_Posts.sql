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
        LikeCount           INT                NOT NULL CONSTRAINT DF_Posts_LikeCount DEFAULT (0),
        IsImportedData      BIT                NOT NULL CONSTRAINT DF_Posts_IsImportedData DEFAULT (0),
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
    -- Supports IPostRepository.HasPostInThread, used to decide whether a new
    -- post makes its author a new distinct participant on the thread.
    CREATE NONCLUSTERED INDEX IX_Posts_ThreadId_CreatedByUserId ON dbo.Posts (ThreadId, CreatedByUserId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Posts') AND name = 'LikeCount')
BEGIN
    ALTER TABLE dbo.Posts ADD LikeCount INT NOT NULL CONSTRAINT DF_Posts_LikeCount DEFAULT (0);

    -- One-time backfill from the data this column replaces - matches what
    -- usp_PostLike_GetCount used to compute with COUNT(*) on every read.
    -- Run via EXEC() so the reference to the column just added above is
    -- resolved at execution time, not batch-parse time (SQL Server
    -- validates column names for the whole batch up front, before the
    -- ALTER above has actually run).
    EXEC(N'
        UPDATE p
        SET p.LikeCount = (SELECT COUNT(*) FROM dbo.PostLikes AS pl WHERE pl.PostId = p.Id)
        FROM dbo.Posts AS p;
    ');
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Posts_ThreadId_CreatedByUserId' AND object_id = OBJECT_ID('dbo.Posts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Posts_ThreadId_CreatedByUserId ON dbo.Posts (ThreadId, CreatedByUserId);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Posts') AND name = 'IsImportedData')
BEGIN
    ALTER TABLE dbo.Posts ADD IsImportedData BIT NOT NULL CONSTRAINT DF_Posts_IsImportedData DEFAULT (0);
END
GO
