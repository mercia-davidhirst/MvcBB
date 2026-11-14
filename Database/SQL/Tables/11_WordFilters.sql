/*
    Table:   dbo.WordFilters
    Purpose: Backing store for MvcBB.Shared.Models.WordFilter.WordFilterModel /
             IWordFilterRepository.
    Notes:
      - Applied once, at write time, to a thread's Title and a post's
        Content before either is saved (see ThreadsController.CreateThread
        and PostsController.CreatePost/UpdatePost in MvcBB.API) - not at
        render time - so the stored data is already clean.
*/

IF OBJECT_ID('dbo.WordFilters') IS NULL
BEGIN
    CREATE TABLE dbo.WordFilters
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        Word            NVARCHAR(100)      NOT NULL,
        Replacement     NVARCHAR(100)      NOT NULL CONSTRAINT DF_WordFilters_Replacement DEFAULT (N''),
        IsActive        BIT                NOT NULL CONSTRAINT DF_WordFilters_IsActive DEFAULT (1),
        SortOrder       INT                NOT NULL CONSTRAINT DF_WordFilters_SortOrder DEFAULT (0),
        CONSTRAINT PK_WordFilters PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_WordFilters_Word UNIQUE (Word)
    );

    CREATE NONCLUSTERED INDEX IX_WordFilters_SortOrder ON dbo.WordFilters (SortOrder);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.WordFilters') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.WordFilters ADD NewColumn ...;
--   END
--   GO
