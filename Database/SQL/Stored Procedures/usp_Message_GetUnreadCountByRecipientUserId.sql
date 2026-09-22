-- Matches IMessageRepository.GetUnreadCountByRecipientUserId(string userId)

DROP PROCEDURE IF EXISTS dbo.usp_Message_GetUnreadCountByRecipientUserId;
GO

CREATE PROCEDURE dbo.usp_Message_GetUnreadCountByRecipientUserId
    @RecipientUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS UnreadCount
    FROM dbo.Messages
    WHERE RecipientUserId = @RecipientUserId
      AND ReadAt IS NULL;
END;
GO
