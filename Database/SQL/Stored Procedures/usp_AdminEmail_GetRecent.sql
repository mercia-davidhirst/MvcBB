-- Matches IAdminEmailRepository.GetRecent(int take)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmail_GetRecent;
GO

CREATE PROCEDURE dbo.usp_AdminEmail_GetRecent
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) *
    FROM dbo.AdminEmails
    ORDER BY CreatedAt DESC, Id DESC;
END;
GO
