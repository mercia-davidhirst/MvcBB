-- Matches IAdminEmailRepository.IsOptedOut(int userId)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmailOptOut_Get;
GO

CREATE PROCEDURE dbo.usp_AdminEmailOptOut_Get
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.AdminEmailOptOuts WHERE UserId = @UserId) THEN 1 ELSE 0 END AS BIT);
END;
GO
