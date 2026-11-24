-- Matches IAdminEmailRepository.GetRecipients(UserRole? role, int? userId,
-- bool includeOptedOut). Deleted (Role 3) accounts and blank emails are
-- never included.

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmail_GetRecipients;
GO

CREATE PROCEDURE dbo.usp_AdminEmail_GetRecipients
    @Role TINYINT = NULL,
    @UserId INT = NULL,
    @IncludeOptedOut BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.Id AS UserId, u.Username, u.Email
    FROM dbo.Users u
    WHERE u.Role <> 3
      AND u.Email <> N''
      AND (@Role IS NULL OR u.Role = @Role)
      AND (@UserId IS NULL OR u.Id = @UserId)
      AND (@IncludeOptedOut = 1 OR NOT EXISTS (SELECT 1 FROM dbo.AdminEmailOptOuts o WHERE o.UserId = u.Id))
    ORDER BY u.Id;
END;
GO
