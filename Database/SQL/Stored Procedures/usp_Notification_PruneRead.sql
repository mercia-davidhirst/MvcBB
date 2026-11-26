-- Matches INotificationRepository.PruneRead(DateTime createdBefore)
-- Deletes read notifications older than the cutoff; returns how many went.

DROP PROCEDURE IF EXISTS dbo.usp_Notification_PruneRead;
GO

CREATE PROCEDURE dbo.usp_Notification_PruneRead
    @CreatedBefore DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Notifications WHERE IsRead = 1 AND CreatedAt < @CreatedBefore;
    SELECT @@ROWCOUNT;
END;
GO
