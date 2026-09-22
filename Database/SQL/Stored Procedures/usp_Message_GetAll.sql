-- Matches IMessageRepository.GetAll()

DROP PROCEDURE IF EXISTS dbo.usp_Message_GetAll;
GO

CREATE PROCEDURE dbo.usp_Message_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Subject, Content, SenderUserId, RecipientUserId, CreatedAt, ReadAt
    FROM dbo.Messages;
END;
GO
