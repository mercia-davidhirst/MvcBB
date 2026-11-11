-- Matches IAnnouncementManagementService.GetActiveAnnouncements()

DROP PROCEDURE IF EXISTS dbo.usp_Announcement_GetActive;
GO

CREATE PROCEDURE dbo.usp_Announcement_GetActive
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Content, IsActive, SortOrder, StartDate, EndDate, CreatedAt
    FROM dbo.Announcements
    WHERE IsActive = 1
        AND (StartDate IS NULL OR StartDate <= SYSUTCDATETIME())
        AND (EndDate IS NULL OR EndDate >= SYSUTCDATETIME())
    ORDER BY SortOrder;
END;
GO
