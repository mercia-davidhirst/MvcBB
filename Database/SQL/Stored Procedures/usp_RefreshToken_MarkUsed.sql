-- Matches IRefreshTokenRepository.MarkUsed(int id, DateTime usedAt)
-- Only marks a token that is still unused and not revoked, in one statement,
-- so two requests can never both spend the same token. Returns 1 if this
-- call marked it, 0 if it was already spent or revoked.

DROP PROCEDURE IF EXISTS dbo.usp_RefreshToken_MarkUsed;
GO

CREATE PROCEDURE dbo.usp_RefreshToken_MarkUsed
    @Id INT,
    @UsedAt DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET UsedAt = @UsedAt
    WHERE Id = @Id AND UsedAt IS NULL AND RevokedAt IS NULL;

    SELECT @@ROWCOUNT;
END;
GO
