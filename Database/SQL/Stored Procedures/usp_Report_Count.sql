-- Matches IReportRepository.Count()

DROP PROCEDURE IF EXISTS dbo.usp_Report_Count;
GO

CREATE PROCEDURE dbo.usp_Report_Count
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS ReportCount
    FROM dbo.Reports;
END;
GO
