-- Matches IAdminEmailRepository.UpdateProgress(AdminEmail email)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmail_UpdateProgress;
GO

CREATE PROCEDURE dbo.usp_AdminEmail_UpdateProgress
    @Id INT,
    @Status TINYINT,
    @RecipientCount INT,
    @SentCount INT,
    @FailedCount INT,
    @CompletedAt DATETIME2(3) = NULL,
    @PausedUntil DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.AdminEmails
    SET Status = @Status,
        RecipientCount = @RecipientCount,
        SentCount = @SentCount,
        FailedCount = @FailedCount,
        CompletedAt = @CompletedAt,
        PausedUntil = @PausedUntil
    WHERE Id = @Id;
END;
GO
