/*
    Table:   dbo.Ranks
    Purpose: Backing store for MvcBB.Shared.Models.Rank.RankModel /
             IRankRepository.
    Notes:
      - A user's rank is never stored against them - RankCalculator.GetRank
        computes it on the fly from their live post count against MinPosts,
        so it always stays in sync with reality.
*/

IF OBJECT_ID('dbo.Ranks') IS NULL
BEGIN
    CREATE TABLE dbo.Ranks
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        Title           NVARCHAR(70)       NOT NULL,
        MinPosts        INT                NOT NULL CONSTRAINT DF_Ranks_MinPosts DEFAULT (0),
        ImagePath       NVARCHAR(500)      NULL,
        IsActive        BIT                NOT NULL CONSTRAINT DF_Ranks_IsActive DEFAULT (1),
        SortOrder       INT                NOT NULL CONSTRAINT DF_Ranks_SortOrder DEFAULT (0),
        CONSTRAINT PK_Ranks PRIMARY KEY CLUSTERED (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Ranks_MinPosts ON dbo.Ranks (MinPosts);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ranks') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Ranks ADD NewColumn ...;
--   END
--   GO
