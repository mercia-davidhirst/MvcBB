-- Matches IThreadSubscriptionRepository.Add(ThreadSubscriptionModel subscription)

DROP PROCEDURE IF EXISTS dbo.usp_ThreadSubscription_Insert;
GO

CREATE PROCEDURE dbo.usp_ThreadSubscription_Insert
    @ThreadId INT,
    @UserId INT,
    @NotifyViaMessage BIT = 1,
    @NotifyViaEmail BIT = 0,
    @CreatedAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ThreadSubscriptions (ThreadId, UserId, NotifyViaMessage, NotifyViaEmail, CreatedAt)
    OUTPUT inserted.Id, inserted.ThreadId, inserted.UserId, inserted.NotifyViaMessage, inserted.NotifyViaEmail, inserted.CreatedAt
    VALUES (@ThreadId, @UserId, @NotifyViaMessage, @NotifyViaEmail, ISNULL(@CreatedAt, SYSUTCDATETIME()));
END;
GO
