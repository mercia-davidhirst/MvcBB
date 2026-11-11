-- Matches IAnnouncementManagementService.GetAnnouncement(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Announcement_GetById;
GO

CREATE PROCEDURE dbo.usp_Announcement_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, Content, IsActive, SortOrder, StartDate, EndDate, CreatedAt
    FROM dbo.Announcements
    WHERE Id = @Id;
END;
GO
