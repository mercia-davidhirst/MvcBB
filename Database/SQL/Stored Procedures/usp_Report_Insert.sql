-- Matches IReportRepository.Add(Report report)
-- Validates the polymorphic ContentId/Type pair via dbo.ufn_Report_ContentExists
-- before inserting, since no ordinary foreign key can express that relationship.

DROP PROCEDURE IF EXISTS dbo.usp_Report_Insert;
GO

CREATE PROCEDURE dbo.usp_Report_Insert
    @Reason NVARCHAR(500),
    @ReporterUserId INT,
    @CreatedAt DATETIME2(3) = NULL,
    @Type TINYINT,
    @ContentId INT,
    @Status TINYINT = 0,
    @ModeratorNotes NVARCHAR(1000) = N''
AS
BEGIN
    SET NOCOUNT ON;

    IF dbo.ufn_Report_ContentExists(@Type, @ContentId) = 0
    BEGIN
        RAISERROR('ContentId %d does not exist for report Type %d.', 16, 1, @ContentId, @Type);
        RETURN;
    END

    INSERT INTO dbo.Reports (Reason, ReporterUserId, CreatedAt, Type, ContentId, Status, ModeratorNotes)
    OUTPUT inserted.Id, inserted.Reason, inserted.ReporterUserId, inserted.CreatedAt, inserted.Type,
           inserted.ContentId, inserted.Status, inserted.ModeratorNotes, inserted.ResolvedAt, inserted.ResolvedByUserId
    VALUES (@Reason, @ReporterUserId, ISNULL(@CreatedAt, SYSUTCDATETIME()), @Type, @ContentId, @Status, @ModeratorNotes);
END;
GO
