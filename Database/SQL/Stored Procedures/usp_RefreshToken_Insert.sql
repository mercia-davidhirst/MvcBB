-- Matches IRefreshTokenRepository.Add(RefreshToken token)

DROP PROCEDURE IF EXISTS dbo.usp_RefreshToken_Insert;
GO

CREATE PROCEDURE dbo.usp_RefreshToken_Insert
    @UserId INT,
    @TokenHash CHAR(64),
    @FamilyId UNIQUEIDENTIFIER,
    @SessionStamp VARCHAR(64),
    @ExpiresAt DATETIME2(3),
    @CreatedAt DATETIME2(3) = NULL,
    @CreatedIp VARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RefreshTokens (UserId, TokenHash, FamilyId, SessionStamp, CreatedAt, ExpiresAt, CreatedIp)
    OUTPUT inserted.Id, inserted.UserId, inserted.TokenHash, inserted.FamilyId, inserted.SessionStamp,
           inserted.CreatedAt, inserted.ExpiresAt, inserted.UsedAt, inserted.RevokedAt, inserted.CreatedIp
    VALUES (@UserId, @TokenHash, @FamilyId, @SessionStamp, ISNULL(@CreatedAt, SYSUTCDATETIME()), @ExpiresAt, @CreatedIp);
END;
GO
