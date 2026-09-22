-- Matches IMessageRepository.Update(Message message)
-- In practice only ReadAt changes post-creation (see MessagesController marking
-- a message read), but the repository's Update takes the whole object, so this
-- allows updating all mutable fields.

DROP PROCEDURE IF EXISTS dbo.usp_Message_Update;
GO

CREATE PROCEDURE dbo.usp_Message_Update
    @Id INT,
    @Subject NVARCHAR(200),
    @Content NVARCHAR(MAX),
    @ReadAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Messages
    SET Subject = @Subject,
        Content = @Content,
        ReadAt = @ReadAt
    WHERE Id = @Id;
END;
GO
