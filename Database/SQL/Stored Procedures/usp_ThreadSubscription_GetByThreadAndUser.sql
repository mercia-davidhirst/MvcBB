-- Matches IThreadSubscriptionRepository.GetByThreadAndUser(int threadId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_ThreadSubscription_GetByThreadAndUser;
GO

CREATE PROCEDURE dbo.usp_ThreadSubscription_GetByThreadAndUser
    @ThreadId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ThreadId, UserId, NotifyViaMessage, NotifyViaEmail, CreatedAt
    FROM dbo.ThreadSubscriptions
    WHERE ThreadId = @ThreadId AND UserId = @UserId;
END;
GO
