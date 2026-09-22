-- Matches IReportRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Report_GetById;
GO

CREATE PROCEDURE dbo.usp_Report_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Reason, ReporterUserId, CreatedAt, Type, ContentId, Status,
           ModeratorNotes, ResolvedAt, ResolvedByUserId
    FROM dbo.Reports
    WHERE Id = @Id;
END;
GO
