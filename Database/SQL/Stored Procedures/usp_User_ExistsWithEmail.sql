-- Matches IUserRepository.ExistsWithEmail(string email, int? excludeUserId = null)

DROP PROCEDURE IF EXISTS dbo.usp_User_ExistsWithEmail;
GO

CREATE PROCEDURE dbo.usp_User_ExistsWithEmail
    @Email NVARCHAR(256),
    @ExcludeUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Users
        WHERE Email = @Email
          AND (@ExcludeUserId IS NULL OR Id <> @ExcludeUserId)
    ) THEN 1 ELSE 0 END AS [Exists];
END;
GO
