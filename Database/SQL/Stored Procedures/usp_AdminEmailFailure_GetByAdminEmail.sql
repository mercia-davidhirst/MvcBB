-- Matches IAdminEmailRepository.GetFailures(int adminEmailId)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmailFailure_GetByAdminEmail;
GO

CREATE PROCEDURE dbo.usp_AdminEmailFailure_GetByAdminEmail
    @AdminEmailId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.AdminEmailFailures
    WHERE AdminEmailId = @AdminEmailId
    ORDER BY FailedAt, Id;
END;
GO
