-- Matches IRefreshTokenRepository.RevokeFamily(Guid familyId)
-- Ends one "Remember me" sign-in (every token rotated from it).

DROP PROCEDURE IF EXISTS dbo.usp_RefreshToken_RevokeFamily;
GO

CREATE PROCEDURE dbo.usp_RefreshToken_RevokeFamily
    @FamilyId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET RevokedAt = SYSUTCDATETIME()
    WHERE FamilyId = @FamilyId AND RevokedAt IS NULL;
END;
GO
