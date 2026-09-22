-- Matches IUserRepository.GetByUsername(string username)
-- Comparison relies on a case-insensitive column/database collation, matching
-- the app's StringComparison.OrdinalIgnoreCase semantics.

DROP PROCEDURE IF EXISTS dbo.usp_User_GetByUsername;
GO

CREATE PROCEDURE dbo.usp_User_GetByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Username, PasswordHash, Email, CreatedAt, LastLoginAt, Role,
           Signature, Bio, AvatarUrl, ShowEmail
    FROM dbo.Users
    WHERE Username = @Username;
END;
GO
