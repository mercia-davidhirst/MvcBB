-- Matches IPasswordResetTokenRepository.GetByTokenHash(string tokenHash)

DROP PROCEDURE IF EXISTS dbo.usp_PasswordResetToken_GetByTokenHash;
GO

CREATE PROCEDURE dbo.usp_PasswordResetToken_GetByTokenHash
    @TokenHash CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, UserId, TokenHash, CreatedAt, ExpiresAt, UsedAt
    FROM dbo.PasswordResetTokens
    WHERE TokenHash = @TokenHash;
END;
GO
