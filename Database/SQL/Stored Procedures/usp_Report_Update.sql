-- Matches IReportRepository.Update(Report report)

DROP PROCEDURE IF EXISTS dbo.usp_Report_Update;
GO

CREATE PROCEDURE dbo.usp_Report_Update
    @Id INT,
    @Status TINYINT,
    @ModeratorNotes NVARCHAR(1000),
    @ResolvedAt DATETIME2(3) = NULL,
    @ResolvedByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Reports
    SET Status = @Status,
        ModeratorNotes = @ModeratorNotes,
        ResolvedAt = @ResolvedAt,
        ResolvedByUserId = @ResolvedByUserId
    WHERE Id = @Id;
END;
GO
