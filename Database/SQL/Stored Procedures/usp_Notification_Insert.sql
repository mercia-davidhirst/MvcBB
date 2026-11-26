-- Matches INotificationRepository.Add(Notification notification)

DROP PROCEDURE IF EXISTS dbo.usp_Notification_Insert;
GO

CREATE PROCEDURE dbo.usp_Notification_Insert
    @UserId INT,
    @Type TINYINT,
    @ActorUserId INT = NULL,
    @ThreadId INT = NULL,
    @PostId INT = NULL,
    @MessageId INT = NULL,
    @Excerpt NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Notifications (UserId, Type, ActorUserId, ThreadId, PostId, MessageId, Excerpt)
    OUTPUT inserted.Id
    VALUES (@UserId, @Type, @ActorUserId, @ThreadId, @PostId, @MessageId, @Excerpt);
END;
GO
