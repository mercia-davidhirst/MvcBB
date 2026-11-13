-- Matches IUserRepository.GetMembersPage's total count (the full filtered
-- count, not just the page returned) - a separate call rather than a
-- window function, same reasoning as usp_Thread_GetPageCount.

DROP PROCEDURE IF EXISTS dbo.usp_User_GetMembersPageCount;
GO

CREATE PROCEDURE dbo.usp_User_GetMembersPageCount
    @Role TINYINT = NULL,
    @Search NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(MAX) = NULL;
    IF @Search IS NOT NULL AND LEN(@Search) > 0
        SET @Pattern = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Search), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT COUNT(*)
    FROM dbo.Users
    WHERE (@Role IS NULL OR Role = @Role)
      AND (@Pattern IS NULL OR LOWER(Username) LIKE @Pattern);
END;
GO
