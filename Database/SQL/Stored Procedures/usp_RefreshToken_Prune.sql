-- Matches IRefreshTokenRepository.Prune(DateTime now, DateTime spentBefore)
-- Deletes expired tokens, and used or revoked tokens older than the cutoff;
-- returns how many went.

DROP PROCEDURE IF EXISTS dbo.usp_RefreshToken_Prune;
GO

CREATE PROCEDURE dbo.usp_RefreshToken_Prune
    @Now DATETIME2(3),
    @SpentBefore DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.RefreshTokens
    WHERE ExpiresAt < @Now
       OR UsedAt < @SpentBefore
       OR RevokedAt < @SpentBefore;

    SELECT @@ROWCOUNT;
END;
GO
