/*
    Table:   dbo.Boards
    Purpose: Backing store for MvcBB.Shared.Models.Board.Board / IBoardRepository.
    Notes:
      - ThreadCount, PostCount, LastPostAt and LastPostByUserId are maintained
        by the application layer (see ThreadsController.CreateThread /
        DeleteThread, which read-modify-write these fields directly), not by
        database triggers, so they are plain columns here.
*/

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Boards');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Boards;
GO

CREATE TABLE dbo.Boards
(
    Id                  INT IDENTITY(1,1)  NOT NULL,
    Name                NVARCHAR(100)      NOT NULL,
    Description         NVARCHAR(500)      NOT NULL CONSTRAINT DF_Boards_Description DEFAULT (N''),
    SortOrder           INT                NOT NULL CONSTRAINT DF_Boards_SortOrder DEFAULT (0),
    IsActive            BIT                NOT NULL CONSTRAINT DF_Boards_IsActive DEFAULT (1),
    CreatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_Boards_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(3)       NULL,
    ThreadCount         INT                NOT NULL CONSTRAINT DF_Boards_ThreadCount DEFAULT (0),
    PostCount           INT                NOT NULL CONSTRAINT DF_Boards_PostCount DEFAULT (0),
    LastPostByUserId    INT                NULL,
    LastPostAt          DATETIME2(3)       NULL,
    CONSTRAINT PK_Boards PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Boards_LastPostByUser FOREIGN KEY (LastPostByUserId) REFERENCES dbo.Users (Id)
);
GO

CREATE NONCLUSTERED INDEX IX_Boards_SortOrder ON dbo.Boards (SortOrder);
GO
