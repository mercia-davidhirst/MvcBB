-- Matches IThreadSubscriptionRepository.Update(ThreadSubscriptionModel subscription)

DROP PROCEDURE IF EXISTS dbo.usp_ThreadSubscription_Update;
GO

CREATE PROCEDURE dbo.usp_ThreadSubscription_Update
    @Id INT,
    @NotifyViaMessage BIT,
    @NotifyViaEmail BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ThreadSubscriptions
    SET NotifyViaMessage = @NotifyViaMessage,
        NotifyViaEmail = @NotifyViaEmail
    WHERE Id = @Id;
END;
GO
