-- Matches IPasswordPolicyRepository.Get()

DROP PROCEDURE IF EXISTS dbo.usp_PasswordPolicy_Get;
GO

CREATE PROCEDURE dbo.usp_PasswordPolicy_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT MinimumLength, RequireUppercase, RequireLowercase, RequireDigit, RequireSpecialCharacter
    FROM dbo.PasswordPolicy
    WHERE Id = 1;
END;
GO
