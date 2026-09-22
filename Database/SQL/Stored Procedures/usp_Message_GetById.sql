-- Matches IMessageRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Message_GetById;
GO

CREATE PROCEDURE dbo.usp_Message_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Subject, Content, SenderUserId, RecipientUserId, CreatedAt, ReadAt
    FROM dbo.Messages
    WHERE Id = @Id;
END;
GO
