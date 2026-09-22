-- Matches IMessageRepository.Add(Message message)

DROP PROCEDURE IF EXISTS dbo.usp_Message_Insert;
GO

CREATE PROCEDURE dbo.usp_Message_Insert
    @Subject NVARCHAR(200) = N'',
    @Content NVARCHAR(MAX),
    @SenderUserId INT,
    @RecipientUserId INT,
    @CreatedAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Messages (Subject, Content, SenderUserId, RecipientUserId, CreatedAt)
    OUTPUT inserted.Id, inserted.Subject, inserted.Content, inserted.SenderUserId,
           inserted.RecipientUserId, inserted.CreatedAt, inserted.ReadAt
    VALUES (@Subject, @Content, @SenderUserId, @RecipientUserId, ISNULL(@CreatedAt, SYSUTCDATETIME()));
END;
GO
