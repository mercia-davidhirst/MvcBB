-- Matches IPollRepository.GetByThreadId(int threadId)

DROP PROCEDURE IF EXISTS dbo.usp_Poll_GetByThreadId;
GO

CREATE PROCEDURE dbo.usp_Poll_GetByThreadId
    @ThreadId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ThreadId, Question, CreatedAt
    FROM dbo.Polls
    WHERE ThreadId = @ThreadId;
END;
GO
