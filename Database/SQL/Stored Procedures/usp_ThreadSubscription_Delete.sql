-- Matches IThreadSubscriptionRepository.Remove(int threadId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_ThreadSubscription_Delete;
GO

CREATE PROCEDURE dbo.usp_ThreadSubscription_Delete
    @ThreadId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ThreadSubscriptions
    WHERE ThreadId = @ThreadId AND UserId = @UserId;
END;
GO
