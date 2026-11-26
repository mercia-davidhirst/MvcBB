-- Matches INotificationRepository.GetByUser(int userId, int skip, int take)
-- and GetById(int id) (pass @Id; @UserId is then ignored).
-- Newest first, with the actor's username and the thread's current title.
-- TotalCount is the member's full notification count, on every row.

DROP PROCEDURE IF EXISTS dbo.usp_Notification_GetByUser;
GO

CREATE PROCEDURE dbo.usp_Notification_GetByUser
    @UserId INT = NULL,
    @Skip INT = 0,
    @Take INT = 25,
    @Id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT n.Id, n.UserId, n.Type, n.ActorUserId, a.Username AS ActorUsername,
           n.ThreadId, t.Title AS ThreadTitle, n.PostId, n.MessageId, n.Excerpt,
           n.IsRead, n.CreatedAt,
           COUNT(*) OVER () AS TotalCount
    FROM dbo.Notifications AS n
    LEFT JOIN dbo.Users AS a ON a.Id = n.ActorUserId
    LEFT JOIN dbo.Threads AS t ON t.Id = n.ThreadId
    WHERE (@Id IS NOT NULL AND n.Id = @Id)
       OR (@Id IS NULL AND n.UserId = @UserId)
    ORDER BY n.CreatedAt DESC, n.Id DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
