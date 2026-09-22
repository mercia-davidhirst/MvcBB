-- Matches IReportRepository.CountByStatus(ReportStatus status)

DROP PROCEDURE IF EXISTS dbo.usp_Report_CountByStatus;
GO

CREATE PROCEDURE dbo.usp_Report_CountByStatus
    @Status TINYINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS ReportCount
    FROM dbo.Reports
    WHERE Status = @Status;
END;
GO
