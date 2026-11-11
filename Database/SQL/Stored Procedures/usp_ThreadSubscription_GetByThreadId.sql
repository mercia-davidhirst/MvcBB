-- Matches IThreadSubscriptionRepository.GetByThreadId(int threadId)

DROP PROCEDURE IF EXISTS dbo.usp_ThreadSubscription_GetByThreadId;
GO

CREATE PROCEDURE dbo.usp_ThreadSubscription_GetByThreadId
    @ThreadId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, ThreadId, UserId, NotifyViaMessage, NotifyViaEmail, CreatedAt
    FROM dbo.ThreadSubscriptions
    WHERE ThreadId = @ThreadId;
END;
GO
