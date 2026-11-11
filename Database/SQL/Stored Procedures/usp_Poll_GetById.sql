-- Matches IPollRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Poll_GetById;
GO

CREATE PROCEDURE dbo.usp_Poll_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ThreadId, Question, CreatedAt
    FROM dbo.Polls
    WHERE Id = @Id;
END;
GO
