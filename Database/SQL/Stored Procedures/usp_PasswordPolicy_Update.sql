-- Matches IPasswordPolicyRepository.Update(PasswordPolicy policy)

DROP PROCEDURE IF EXISTS dbo.usp_PasswordPolicy_Update;
GO

CREATE PROCEDURE dbo.usp_PasswordPolicy_Update
    @MinimumLength INT,
    @RequireUppercase BIT,
    @RequireLowercase BIT,
    @RequireDigit BIT,
    @RequireSpecialCharacter BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PasswordPolicy
    SET MinimumLength = @MinimumLength,
        RequireUppercase = @RequireUppercase,
        RequireLowercase = @RequireLowercase,
        RequireDigit = @RequireDigit,
        RequireSpecialCharacter = @RequireSpecialCharacter
    WHERE Id = 1;
END;
GO
