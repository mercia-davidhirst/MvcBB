-- Matches IUserRepository.ExistsWithUsername(string username, int? excludeUserId = null)

DROP PROCEDURE IF EXISTS dbo.usp_User_ExistsWithUsername;
GO

CREATE PROCEDURE dbo.usp_User_ExistsWithUsername
    @Username NVARCHAR(50),
    @ExcludeUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Users
        WHERE Username = @Username
          AND (@ExcludeUserId IS NULL OR Id <> @ExcludeUserId)
    ) THEN 1 ELSE 0 END AS [Exists];
END;
GO
