-- Matches IUserRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_User_GetById;
GO

CREATE PROCEDURE dbo.usp_User_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Username, PasswordHash, Email, CreatedAt, LastLoginAt, Role,
           Signature, Bio, AvatarUrl, ShowEmail
    FROM dbo.Users
    WHERE Id = @Id;
END;
GO
