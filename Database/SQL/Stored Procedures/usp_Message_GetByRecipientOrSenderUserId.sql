-- Matches IMessageRepository.GetByRecipientOrSenderUserId(string userId)

DROP PROCEDURE IF EXISTS dbo.usp_Message_GetByRecipientOrSenderUserId;
GO

CREATE PROCEDURE dbo.usp_Message_GetByRecipientOrSenderUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Subject, Content, SenderUserId, RecipientUserId, CreatedAt, ReadAt
    FROM dbo.Messages
    WHERE RecipientUserId = @UserId OR SenderUserId = @UserId
    ORDER BY CreatedAt DESC;
END;
GO
