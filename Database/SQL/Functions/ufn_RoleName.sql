/*
    Function: dbo.ufn_RoleName
    Purpose:  Converts a Users.Role TINYINT (matching
              MvcBB.Shared.Models.User.UserRole) into the display string used
              by MvcBB.Shared.Models.Post.Post.UserRole. Used by
              dbo.vw_PostDetails.
*/

DROP FUNCTION IF EXISTS dbo.ufn_RoleName;
GO

CREATE FUNCTION dbo.ufn_RoleName (@Role TINYINT)
RETURNS NVARCHAR(20)
AS
BEGIN
    RETURN CASE @Role
        WHEN 0 THEN N'User'
        WHEN 1 THEN N'Moderator'
        WHEN 2 THEN N'Administrator'
        ELSE NULL
    END;
END;
GO
