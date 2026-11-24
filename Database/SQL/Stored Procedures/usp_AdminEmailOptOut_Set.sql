-- Matches IAdminEmailRepository.SetOptedOut(int userId, bool optedOut)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmailOptOut_Set;
GO

CREATE PROCEDURE dbo.usp_AdminEmailOptOut_Set
    @UserId INT,
    @OptedOut BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF @OptedOut = 1
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.AdminEmailOptOuts WHERE UserId = @UserId)
            INSERT INTO dbo.AdminEmailOptOuts (UserId) VALUES (@UserId);
    END
    ELSE
    BEGIN
        DELETE FROM dbo.AdminEmailOptOuts WHERE UserId = @UserId;
    END
END;
GO
