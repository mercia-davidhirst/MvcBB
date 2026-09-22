-- Matches IMessageRepository.GetByRecipientUserId(string userId, bool unreadOnly = false)

DROP PROCEDURE IF EXISTS dbo.usp_Message_GetByRecipientUserId;
GO

CREATE PROCEDURE dbo.usp_Message_GetByRecipientUserId
    @RecipientUserId INT,
    @UnreadOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Subject, Content, SenderUserId, RecipientUserId, CreatedAt, ReadAt
    FROM dbo.Messages
    WHERE RecipientUserId = @RecipientUserId
      AND (@UnreadOnly = 0 OR ReadAt IS NULL)
    ORDER BY CreatedAt DESC;
END;
GO
