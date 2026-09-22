-- Matches IReportRepository.GetAll() (ordered by CreatedAt desc, as in InMemoryReportRepository)

DROP PROCEDURE IF EXISTS dbo.usp_Report_GetAll;
GO

CREATE PROCEDURE dbo.usp_Report_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Reason, ReporterUserId, CreatedAt, Type, ContentId, Status,
           ModeratorNotes, ResolvedAt, ResolvedByUserId
    FROM dbo.Reports
    ORDER BY CreatedAt DESC;
END;
GO
