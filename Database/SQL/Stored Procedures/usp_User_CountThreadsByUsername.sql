-- Matches IUserRepository.CountThreadsByUsername(string username)
-- Threads.CreatedByUserId stores the author's numeric Users.Id, same as posts
-- (see usp_User_CountPostsByUsername for the reasoning).

DROP PROCEDURE IF EXISTS dbo.usp_User_CountThreadsByUsername;
GO

CREATE PROCEDURE dbo.usp_User_CountThreadsByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS ThreadCount
    FROM dbo.Threads AS t
    INNER JOIN dbo.Users AS u ON u.Id = t.CreatedByUserId
    WHERE u.Username = @Username;
END;
GO
