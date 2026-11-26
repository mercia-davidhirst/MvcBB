-- Matches IModerationQueueRepository.GetAll() (@Id null: everything, oldest
-- first) and GetById(int id). Adds the author's username, the board's name
-- and, for a reply, the thread's title.

DROP PROCEDURE IF EXISTS dbo.usp_ModerationQueue_Get;
GO

CREATE PROCEDURE dbo.usp_ModerationQueue_Get
    @Id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT q.Id, q.Kind, q.UserId, ISNULL(u.Username, N'') AS Username,
           q.BoardId, b.Name AS BoardName, q.ThreadId, t.Title AS ThreadTitle,
           q.Title, q.Content, q.QuotedPostId, q.ImageUrl, q.PollJson, q.CreatedAt
    FROM dbo.ModerationQueue AS q
    LEFT JOIN dbo.Users AS u ON u.Id = q.UserId
    LEFT JOIN dbo.Boards AS b ON b.Id = q.BoardId
    LEFT JOIN dbo.Threads AS t ON t.Id = q.ThreadId
    WHERE @Id IS NULL OR q.Id = @Id
    ORDER BY q.CreatedAt, q.Id;
END;
GO
