-- Matches IUserRepository.CountPostsByUsername(string username)
-- Posts.CreatedByUserId stores the author's numeric Users.Id (see
-- ClaimTypes.NameIdentifier = user.Id.ToString() in UsersController), so the
-- username parameter is resolved via a join rather than a direct column match.
-- Soft-deleted posts (IsDeleted = 1) are excluded from the count.

DROP PROCEDURE IF EXISTS dbo.usp_User_CountPostsByUsername;
GO

CREATE PROCEDURE dbo.usp_User_CountPostsByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS PostCount
    FROM dbo.Posts AS p
    INNER JOIN dbo.Users AS u ON u.Id = p.CreatedByUserId
    WHERE u.Username = @Username
      AND p.IsDeleted = 0;
END;
GO
