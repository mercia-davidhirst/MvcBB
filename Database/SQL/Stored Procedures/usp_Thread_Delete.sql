-- Matches IForumThreadRepository.Remove(ForumThread thread)

DROP PROCEDURE IF EXISTS dbo.usp_Thread_Delete;
GO

CREATE PROCEDURE dbo.usp_Thread_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Threads
    WHERE Id = @Id;
END;
GO
