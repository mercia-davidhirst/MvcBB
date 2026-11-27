-- Matches IRefreshTokenRepository.GetByTokenHash(string tokenHash)

DROP PROCEDURE IF EXISTS dbo.usp_RefreshToken_GetByTokenHash;
GO

CREATE PROCEDURE dbo.usp_RefreshToken_GetByTokenHash
    @TokenHash CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, UserId, TokenHash, FamilyId, SessionStamp, CreatedAt, ExpiresAt, UsedAt, RevokedAt, CreatedIp
    FROM dbo.RefreshTokens
    WHERE TokenHash = @TokenHash;
END;
GO
