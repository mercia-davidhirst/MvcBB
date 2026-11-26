-- Matches INotificationRepository.Exists(int userId, NotificationType type, int postId, int? actorUserId)

DROP PROCEDURE IF EXISTS dbo.usp_Notification_Exists;
GO

CREATE PROCEDURE dbo.usp_Notification_Exists
    @UserId INT,
    @Type TINYINT,
    @PostId INT,
    @ActorUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Notifications
        WHERE PostId = @PostId
          AND UserId = @UserId
          AND Type = @Type
          AND (@ActorUserId IS NULL OR ActorUserId = @ActorUserId)
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
END;
GO
