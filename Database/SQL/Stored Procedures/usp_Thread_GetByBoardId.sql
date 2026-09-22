-- Matches IForumThreadRepository.GetByBoardId(int boardId)
-- Ordering matches InMemoryForumThreadRepository: sticky threads first, then
-- most recently active.

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetByBoardId;
GO

CREATE PROCEDURE dbo.usp_Thread_GetByBoardId
    @BoardId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, BoardId, CreatedByUserId, CreatedAt, UpdatedAt, IsSticky,
           IsLocked, ViewCount, PostCount, LastPostAt, LastPostByUserId
    FROM dbo.Threads
    WHERE BoardId = @BoardId
    ORDER BY IsSticky DESC, ISNULL(LastPostAt, CreatedAt) DESC;
END;
GO
