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

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Smilies');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Smilies;
GO

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
GO

CREATE NONCLUSTERED INDEX IX_Smilies_SortOrder ON dbo.Smilies (SortOrder);
GO
