/*
    Table:   dbo.BBCodeTags
    Purpose: Backing store for MvcBB.Shared.Models.BBCode.BBCodeTagModel /
             IBBCodeManagementService's tag methods (GetBBCodeTags,
             GetBBCodeTag, AddBBCodeTag, UpdateBBCodeTag, DeleteBBCodeTag).
    Notes:
      - MvcBB.App.Services.BBCodeService currently holds this list entirely
        in-memory, seeded from a hardcoded set in its constructor. This table
        is where that data belongs once the service is backed by a
        repository; see Seed Data/07_BBCodeTags.sql for the equivalent of
        that hardcoded seed list.
      - Name is unique so the admin tag list (Admin/BBCode) can't end up with
        two ambiguous entries sharing a display name.
*/

IF OBJECT_ID('dbo.BBCodeTags') IS NULL
BEGIN
    CREATE TABLE dbo.BBCodeTags
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        Name            NVARCHAR(100)      NOT NULL,
        Pattern         NVARCHAR(500)      NOT NULL,
        Replacement     NVARCHAR(1000)     NOT NULL,
        Description     NVARCHAR(500)      NOT NULL CONSTRAINT DF_BBCodeTags_Description DEFAULT (N''),
        Example         NVARCHAR(500)      NOT NULL CONSTRAINT DF_BBCodeTags_Example DEFAULT (N''),
        IsActive        BIT                NOT NULL CONSTRAINT DF_BBCodeTags_IsActive DEFAULT (1),
        SortOrder       INT                NOT NULL CONSTRAINT DF_BBCodeTags_SortOrder DEFAULT (0),
        CONSTRAINT PK_BBCodeTags PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_BBCodeTags_Name UNIQUE (Name)
    );

    CREATE NONCLUSTERED INDEX IX_BBCodeTags_SortOrder ON dbo.BBCodeTags (SortOrder);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BBCodeTags') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.BBCodeTags ADD NewColumn ...;
--   END
--   GO
