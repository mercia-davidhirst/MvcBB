-- Matches IAdminEmailRepository.Add(AdminEmail email)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmail_Insert;
GO

CREATE PROCEDURE dbo.usp_AdminEmail_Insert
    @SentByUserId INT,
    @SentByUsername NVARCHAR(50),
    @CreatedAt DATETIME2(3) = NULL,
    @Subject NVARCHAR(200),
    @Body NVARCHAR(MAX),
    @Audience TINYINT,
    @Role TINYINT = NULL,
    @UserId INT = NULL,
    @SenderMode TINYINT,
    @FromAddress NVARCHAR(256),
    @FromName NVARCHAR(256) = NULL,
    @ReplyToAddress NVARCHAR(256) = NULL,
    @IsImportant BIT = 0,
    @Status TINYINT = 0,
    @RecipientCount INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AdminEmails (SentByUserId, SentByUsername, CreatedAt, Subject, Body, Audience, Role, UserId,
                                 SenderMode, FromAddress, FromName, ReplyToAddress, IsImportant, Status, RecipientCount)
    OUTPUT inserted.*
    VALUES (@SentByUserId, @SentByUsername, ISNULL(@CreatedAt, SYSUTCDATETIME()), @Subject, @Body, @Audience, @Role, @UserId,
            @SenderMode, @FromAddress, @FromName, @ReplyToAddress, @IsImportant, @Status, @RecipientCount);
END;
GO
