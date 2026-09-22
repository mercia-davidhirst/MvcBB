/*
    Table:   dbo.Users
    Purpose: Backing store for MvcBB.Shared.Models.User.User / IUserRepository.
    Notes:
      - Username/Email lookups in IUserRepository (GetByUsername, GetByEmail,
        ExistsWithUsername, ExistsWithEmail) are case-insensitive in the app
        (StringComparison.OrdinalIgnoreCase); this relies on the database/column
        using a case-insensitive collation (SQL Server's default CI_AS collations
        already satisfy this).
      - ThreadCount/PostCount on the User model are NOT persisted here: the
        repository exposes CountPostsByUsername/CountThreadsByUsername as
        computed queries (see usp_User_CountPostsByUsername /
        usp_User_CountThreadsByUsername), so storing running totals would just
        be a duplicate of derivable data.
      - Role mirrors MvcBB.Shared.Models.User.UserRole: 0 = User, 1 = Moderator,
        2 = Administrator.
*/

-- Drop any foreign keys pointing at this table first so the table can be
-- dropped/recreated regardless of what else has already been deployed.
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Users');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Users;
GO

CREATE TABLE dbo.Users
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    Username        NVARCHAR(50)       NOT NULL,
    PasswordHash    NVARCHAR(256)      NOT NULL,
    Email           NVARCHAR(256)      NOT NULL,
    CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    LastLoginAt     DATETIME2(3)       NULL,
    Role            TINYINT            NOT NULL CONSTRAINT DF_Users_Role DEFAULT (0),
    Signature       NVARCHAR(1000)     NULL,
    Bio             NVARCHAR(2000)     NULL,
    AvatarUrl       NVARCHAR(500)      NULL,
    ShowEmail       BIT                NOT NULL CONSTRAINT DF_Users_ShowEmail DEFAULT (0),
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN (0, 1, 2))
);
GO
