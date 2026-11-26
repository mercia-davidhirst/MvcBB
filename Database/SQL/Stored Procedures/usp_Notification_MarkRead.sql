-- Matches INotificationRepository.MarkRead, MarkAllRead, MarkReadForThread
-- and MarkReadForMessage: marks the member's unread notifications as read -
-- just @Id, just those about @ThreadId or @MessageId, or (all three null)
-- every one of them. Only ever touches @UserId's own rows.

DROP PROCEDURE IF EXISTS dbo.usp_Notification_MarkRead;
GO

CREATE PROCEDURE dbo.usp_Notification_MarkRead
    @UserId INT,
    @Id INT = NULL,
    @ThreadId INT = NULL,
    @MessageId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Notifications
    SET IsRead = 1
    WHERE UserId = @UserId
      AND IsRead = 0
      AND (@Id IS NULL OR Id = @Id)
      AND (@ThreadId IS NULL OR ThreadId = @ThreadId)
      AND (@MessageId IS NULL OR MessageId = @MessageId);
END;
GO
