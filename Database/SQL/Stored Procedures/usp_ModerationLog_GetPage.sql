-- Matches IModerationLogRepository.GetPage(int skip, int take)
-- Newest first, with both members' usernames. TotalCount is the full log
-- size, on every row.

DROP PROCEDURE IF EXISTS dbo.usp_ModerationLog_GetPage;
GO

CREATE PROCEDURE dbo.usp_ModerationLog_GetPage
    @Skip INT = 0,
    @Take INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT l.Id, l.ActorUserId, a.Username AS ActorUsername, l.Action, l.TargetType,
           l.TargetId, l.TargetUserId, tu.Username AS TargetUsername, l.Summary,
           l.Reason, l.CreatedAt,
           COUNT(*) OVER () AS TotalCount
    FROM dbo.ModerationLog AS l
    LEFT JOIN dbo.Users AS a ON a.Id = l.ActorUserId
    LEFT JOIN dbo.Users AS tu ON tu.Id = l.TargetUserId
    ORDER BY l.CreatedAt DESC, l.Id DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
