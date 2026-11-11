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

DROP TABLE IF EXISTS dbo.WordFilters;
GO

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
GO

CREATE NONCLUSTERED INDEX IX_WordFilters_SortOrder ON dbo.WordFilters (SortOrder);
GO
