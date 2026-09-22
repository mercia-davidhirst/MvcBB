-- Matches IForumThreadRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetById;
GO

CREATE PROCEDURE dbo.usp_Thread_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, BoardId, CreatedByUserId, CreatedAt, UpdatedAt, IsSticky,
           IsLocked, ViewCount, PostCount, LastPostAt, LastPostByUserId
    FROM dbo.Threads
    WHERE Id = @Id;
END;
GO
