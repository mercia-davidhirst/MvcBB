-- Matches IUserRepository.Search(string query, int skip, int take) - filters
-- by username/email, then paginates entirely in SQL instead of the caller
-- loading every user in the table first.

DROP PROCEDURE IF EXISTS dbo.usp_Search_Users;
GO

CREATE PROCEDURE dbo.usp_Search_Users
    @Query NVARCHAR(100),
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(MAX) = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Query), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT Id, Username, PasswordHash, Email, CreatedAt, LastLoginAt, Role,
           Signature, Bio, AvatarUrl, ShowEmail, CustomTitle,
           MustChangePassword, LockedUntil, LastLoginIp
    FROM dbo.Users
    WHERE LOWER(Username) LIKE @Pattern OR LOWER(Email) LIKE @Pattern
    ORDER BY CreatedAt DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
