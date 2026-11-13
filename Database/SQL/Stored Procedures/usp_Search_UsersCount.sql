-- Matches IUserRepository.Search's total count (the full filtered count,
-- not just the page returned) - a separate call rather than a window
-- function, same reasoning as usp_Thread_GetPageCount.

DROP PROCEDURE IF EXISTS dbo.usp_Search_UsersCount;
GO

CREATE PROCEDURE dbo.usp_Search_UsersCount
    @Query NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(MAX) = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Query), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT COUNT(*)
    FROM dbo.Users
    WHERE LOWER(Username) LIKE @Pattern OR LOWER(Email) LIKE @Pattern;
END;
GO
