-- Matches IAnnouncementManagementService.GetAnnouncements()

DROP PROCEDURE IF EXISTS dbo.usp_Announcement_GetAll;
GO

CREATE PROCEDURE dbo.usp_Announcement_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Content, IsActive, SortOrder, StartDate, EndDate, CreatedAt
    FROM dbo.Announcements
    ORDER BY SortOrder;
END;
GO
