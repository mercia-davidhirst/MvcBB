-- Matches IPasswordResetTokenRepository.MarkUsed(int id)

DROP PROCEDURE IF EXISTS dbo.usp_PasswordResetToken_MarkUsed;
GO

CREATE PROCEDURE dbo.usp_PasswordResetToken_MarkUsed
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PasswordResetTokens
    SET UsedAt = SYSUTCDATETIME()
    WHERE Id = @Id;
END;
GO
