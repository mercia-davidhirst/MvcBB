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

IF OBJECT_ID('dbo.PasswordResetTokens') IS NULL
BEGIN
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

    CREATE NONCLUSTERED INDEX IX_PasswordResetTokens_UserId ON dbo.PasswordResetTokens (UserId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PasswordResetTokens') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.PasswordResetTokens ADD NewColumn ...;
--   END
--   GO
