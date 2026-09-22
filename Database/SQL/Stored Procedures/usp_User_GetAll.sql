-- Matches IUserRepository.GetAll()

DROP PROCEDURE IF EXISTS dbo.usp_User_GetAll;
GO

CREATE PROCEDURE dbo.usp_User_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Username, PasswordHash, Email, CreatedAt, LastLoginAt, Role,
           Signature, Bio, AvatarUrl, ShowEmail
    FROM dbo.Users
    ORDER BY Username;
END;
GO
