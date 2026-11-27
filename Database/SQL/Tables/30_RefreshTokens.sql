/*
    Table:   dbo.RefreshTokens
    Purpose: Backing store for MvcBB.Shared.Models.User.RefreshToken /
             IRefreshTokenRepository - "Remember me" sign-ins. Added in v1.4.1.
    Notes:
      - Only TokenHash (a SHA-256 hash, hex-encoded = 64 chars) is stored.
        The raw token exists only in the member's App cookie, so a database
        leak can't be used to sign in as anyone.
      - Tokens are single-use: refreshing marks one used (UsedAt) and issues
        its replacement with the same FamilyId (one family = one sign-in on
        one device). A used token presented again revokes its whole family.
      - SessionStamp is the account's session stamp when the token was
        issued; a password, role or lock change makes it stale.
      - RevokedAt is set on sign-out (that family), and on a password
        change or reset, lock, role change or anonymization (all of the
        member's tokens).
      - Expired tokens, and used or revoked ones over a week old, are
        deleted daily by the API.
*/

IF OBJECT_ID('dbo.RefreshTokens') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        UserId          INT                NOT NULL,
        TokenHash       CHAR(64)           NOT NULL,
        FamilyId        UNIQUEIDENTIFIER   NOT NULL,
        SessionStamp    VARCHAR(64)        NOT NULL,
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ExpiresAt       DATETIME2(3)       NOT NULL,
        UsedAt          DATETIME2(3)       NULL,
        RevokedAt       DATETIME2(3)       NULL,
        CreatedIp       VARCHAR(45)        NULL,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash)
    );

    CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_FamilyId ON dbo.RefreshTokens (FamilyId);
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_ExpiresAt ON dbo.RefreshTokens (ExpiresAt);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here.
