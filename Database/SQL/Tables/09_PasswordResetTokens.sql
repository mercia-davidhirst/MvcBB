/*
    Table:   dbo.PasswordResetTokens
    Purpose: Backing store for MvcBB.Shared.Models.User.PasswordResetToken /
             IPasswordResetTokenRepository.
    Notes:
      - Only TokenHash (a SHA-256 hash, hex-encoded = 64 chars) is ever
        persisted - the raw token only ever exists in the emailed reset link,
        never in the database, so a database leak can't be used to reset
        accounts.
      - A token is single-use: UsedAt is set once consumed, and
        UsersController.ResetPassword rejects anything already used or past
        ExpiresAt.
*/

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.PasswordResetTokens');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.PasswordResetTokens;
GO

CREATE TABLE dbo.PasswordResetTokens
(
    Id          INT IDENTITY(1,1)  NOT NULL,
    UserId      INT                NOT NULL,
    TokenHash   CHAR(64)           NOT NULL,
    CreatedAt   DATETIME2(3)       NOT NULL CONSTRAINT DF_PasswordResetTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
    ExpiresAt   DATETIME2(3)       NOT NULL,
    UsedAt      DATETIME2(3)       NULL,
    CONSTRAINT PK_PasswordResetTokens PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PasswordResetTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id),
    CONSTRAINT UQ_PasswordResetTokens_TokenHash UNIQUE (TokenHash)
);
GO

CREATE NONCLUSTERED INDEX IX_PasswordResetTokens_UserId ON dbo.PasswordResetTokens (UserId);
GO
