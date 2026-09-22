-- Matches IMessageRepository.Remove(Message message)

DROP PROCEDURE IF EXISTS dbo.usp_Message_Delete;
GO

CREATE PROCEDURE dbo.usp_Message_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Messages
    WHERE Id = @Id;
END;
GO
