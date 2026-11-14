/*
    Table:   dbo.Smilies
    Purpose: Backing store for MvcBB.Shared.Models.BBCode.SmilieModel /
             IBBCodeManagementService's smilie methods (GetSmilies,
             GetSmilie, AddSmilie, UpdateSmilie, DeleteSmilie).
    Notes:
      - Same in-memory situation as BBCodeTags: MvcBB.App.Services.BBCodeService
        seeds this list from a hardcoded set in its constructor; see
        Seed Data/08_Smilies.sql for the equivalent seed data.
      - Code is unique (":)"/" :-)" etc are each their own distinct code in
        the current seed list, but two rows sharing the same code would be
        ambiguous for GetAvailableSmilies' dictionary lookup).
*/

IF OBJECT_ID('dbo.Smilies') IS NULL
BEGIN
    CREATE TABLE dbo.Smilies
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        Code            NVARCHAR(20)       NOT NULL,
        Description     NVARCHAR(200)      NOT NULL CONSTRAINT DF_Smilies_Description DEFAULT (N''),
        ImagePath       NVARCHAR(500)      NOT NULL CONSTRAINT DF_Smilies_ImagePath DEFAULT (N''),
        IsActive        BIT                NOT NULL CONSTRAINT DF_Smilies_IsActive DEFAULT (1),
        SortOrder       INT                NOT NULL CONSTRAINT DF_Smilies_SortOrder DEFAULT (0),
        CONSTRAINT PK_Smilies PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Smilies_Code UNIQUE (Code)
    );

    CREATE NONCLUSTERED INDEX IX_Smilies_SortOrder ON dbo.Smilies (SortOrder);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Smilies') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Smilies ADD NewColumn ...;
--   END
--   GO
