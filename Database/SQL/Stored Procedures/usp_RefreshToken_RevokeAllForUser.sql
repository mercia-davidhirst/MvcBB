-- Matches IRefreshTokenRepository.RevokeAllForUser(int userId)
-- Ends every "Remember me" sign-in a member has, on every device.

DROP PROCEDURE IF EXISTS dbo.usp_RefreshToken_RevokeAllForUser;
GO

CREATE PROCEDURE dbo.usp_RefreshToken_RevokeAllForUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET RevokedAt = SYSUTCDATETIME()
    WHERE UserId = @UserId AND RevokedAt IS NULL;
END;
GO
