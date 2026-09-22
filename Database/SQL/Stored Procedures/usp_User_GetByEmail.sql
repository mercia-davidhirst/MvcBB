-- Matches IUserRepository.GetByEmail(string email)

DROP PROCEDURE IF EXISTS dbo.usp_User_GetByEmail;
GO

CREATE PROCEDURE dbo.usp_User_GetByEmail
    @Email NVARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Username, PasswordHash, Email, CreatedAt, LastLoginAt, Role,
           Signature, Bio, AvatarUrl, ShowEmail
    FROM dbo.Users
    WHERE Email = @Email;
END;
GO
