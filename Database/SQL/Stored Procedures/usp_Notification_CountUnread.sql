-- Matches INotificationRepository.CountUnread(int userId)

DROP PROCEDURE IF EXISTS dbo.usp_Notification_CountUnread;
GO

CREATE PROCEDURE dbo.usp_Notification_CountUnread
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.Notifications WHERE UserId = @UserId AND IsRead = 0;
END;
GO
