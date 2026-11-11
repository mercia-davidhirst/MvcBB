-- Matches IAnnouncementManagementService.DeleteAnnouncement(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Announcement_Delete;
GO

CREATE PROCEDURE dbo.usp_Announcement_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Announcements
    WHERE Id = @Id;
END;
GO
