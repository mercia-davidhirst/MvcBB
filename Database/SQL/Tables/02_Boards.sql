/*
    Table:   dbo.Boards
    Purpose: Backing store for MvcBB.Shared.Models.Board.Board / IBoardRepository.
    Notes:
      - ThreadCount, PostCount, LastPostAt and LastPostByUserId are maintained
        by the application layer (see ThreadsController.CreateThread /
        DeleteThread, which read-modify-write these fields directly), not by
        database triggers, so they are plain columns here.
      - ParentId is a self-referencing FK supporting WBB-style nested board
        categories (arbitrary depth); NULL means a top-level board. IsCategory
        marks a pure organizational header with no threads of its own.
      - IsPrivate hides the board (and its threads/posts) from anyone who
        isn't signed in - see Board.IsPrivate / Board.IsVisibleTo.
*/

IF OBJECT_ID('dbo.Boards') IS NULL
BEGIN
    CREATE TABLE dbo.Boards
    (
        Id                  INT IDENTITY(1,1)  NOT NULL,
        Name                NVARCHAR(100)      NOT NULL,
        Description         NVARCHAR(500)      NOT NULL CONSTRAINT DF_Boards_Description DEFAULT (N''),
        SortOrder           INT                NOT NULL CONSTRAINT DF_Boards_SortOrder DEFAULT (0),
        IsActive            BIT                NOT NULL CONSTRAINT DF_Boards_IsActive DEFAULT (1),
        ParentId            INT                NULL,
        IsCategory          BIT                NOT NULL CONSTRAINT DF_Boards_IsCategory DEFAULT (0),
        IsPrivate           BIT                NOT NULL CONSTRAINT DF_Boards_IsPrivate DEFAULT (0),
        CreatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_Boards_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(3)       NULL,
        ThreadCount         INT                NOT NULL CONSTRAINT DF_Boards_ThreadCount DEFAULT (0),
        PostCount           INT                NOT NULL CONSTRAINT DF_Boards_PostCount DEFAULT (0),
        LastPostByUserId    INT                NULL,
        LastPostAt          DATETIME2(3)       NULL,
        CONSTRAINT PK_Boards PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Boards_LastPostByUser FOREIGN KEY (LastPostByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Boards_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Boards (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Boards_SortOrder ON dbo.Boards (SortOrder);
    CREATE NONCLUSTERED INDEX IX_Boards_ParentId ON dbo.Boards (ParentId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Boards') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Boards ADD NewColumn ...;
--   END
--   GO
