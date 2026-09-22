-- Matches IForumThreadRepository.GetAll()

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetAll;
GO

CREATE PROCEDURE dbo.usp_Thread_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, BoardId, CreatedByUserId, CreatedAt, UpdatedAt, IsSticky,
           IsLocked, ViewCount, PostCount, LastPostAt, LastPostByUserId
    FROM dbo.Threads
    ORDER BY IsSticky DESC, ISNULL(LastPostAt, CreatedAt) DESC;
END;
GO
