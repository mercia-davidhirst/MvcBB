-- Matches IAdminEmailRepository.CountRecipients(UserRole? role, int? userId) -
-- the same filter as usp_AdminEmail_GetRecipients, as counts.

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmail_CountRecipients;
GO

CREATE PROCEDURE dbo.usp_AdminEmail_CountRecipients
    @Role TINYINT = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS Total,
           COUNT(o.UserId) AS OptedOut
    FROM dbo.Users u
    LEFT JOIN dbo.AdminEmailOptOuts o ON o.UserId = u.Id
    WHERE u.Role <> 3
      AND u.Email <> N''
      AND (@Role IS NULL OR u.Role = @Role)
      AND (@UserId IS NULL OR u.Id = @UserId);
END;
GO
