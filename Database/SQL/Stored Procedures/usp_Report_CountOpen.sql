-- Matches IReportRepository.CountOpen(): reports still waiting on a
-- moderator, i.e. Status 0 (Pending) or 1 (Investigating). Polled by the
-- moderator/administrator navbar alert badge.

DROP PROCEDURE IF EXISTS dbo.usp_Report_CountOpen;
GO

CREATE PROCEDURE dbo.usp_Report_CountOpen
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS ReportCount
    FROM dbo.Reports
    WHERE Status IN (0, 1);
END;
GO
