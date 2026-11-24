-- Matches IAdminEmailRepository.AddDelivery(int adminEmailId, int userId, DateTime sentAt)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmailDelivery_Insert;
GO

CREATE PROCEDURE dbo.usp_AdminEmailDelivery_Insert
    @AdminEmailId INT,
    @UserId INT,
    @SentAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.AdminEmailDeliveries WHERE AdminEmailId = @AdminEmailId AND UserId = @UserId)
        INSERT INTO dbo.AdminEmailDeliveries (AdminEmailId, UserId, SentAt)
        VALUES (@AdminEmailId, @UserId, ISNULL(@SentAt, SYSUTCDATETIME()));
END;
GO
