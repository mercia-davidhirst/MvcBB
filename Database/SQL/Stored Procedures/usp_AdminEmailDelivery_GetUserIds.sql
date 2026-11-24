-- Matches IAdminEmailRepository.GetDeliveredUserIds(int adminEmailId)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmailDelivery_GetUserIds;
GO

CREATE PROCEDURE dbo.usp_AdminEmailDelivery_GetUserIds
    @AdminEmailId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UserId
    FROM dbo.AdminEmailDeliveries
    WHERE AdminEmailId = @AdminEmailId;
END;
GO
