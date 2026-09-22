-- Matches IReportRepository.CountByType(ReportType type)

DROP PROCEDURE IF EXISTS dbo.usp_Report_CountByType;
GO

CREATE PROCEDURE dbo.usp_Report_CountByType
    @Type TINYINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS ReportCount
    FROM dbo.Reports
    WHERE Type = @Type;
END;
GO
