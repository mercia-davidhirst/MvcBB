-- Matches IModerationQueueRepository.Remove(int id)

DROP PROCEDURE IF EXISTS dbo.usp_ModerationQueue_Delete;
GO

CREATE PROCEDURE dbo.usp_ModerationQueue_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ModerationQueue WHERE Id = @Id;
END;
GO
