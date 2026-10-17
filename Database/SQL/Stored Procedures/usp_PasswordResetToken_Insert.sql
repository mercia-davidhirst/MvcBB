-- Matches IPasswordResetTokenRepository.Add(PasswordResetToken token)

DROP PROCEDURE IF EXISTS dbo.usp_PasswordResetToken_Insert;
GO

CREATE PROCEDURE dbo.usp_PasswordResetToken_Insert
    @UserId INT,
    @TokenHash CHAR(64),
    @ExpiresAt DATETIME2(3),
    @CreatedAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PasswordResetTokens (UserId, TokenHash, CreatedAt, ExpiresAt)
    OUTPUT inserted.Id, inserted.UserId, inserted.TokenHash, inserted.CreatedAt, inserted.ExpiresAt, inserted.UsedAt
    VALUES (@UserId, @TokenHash, ISNULL(@CreatedAt, SYSUTCDATETIME()), @ExpiresAt);
END;
GO
