-- Matches IAnnouncementManagementService.AddAnnouncement(AnnouncementModel model)

DROP PROCEDURE IF EXISTS dbo.usp_Announcement_Insert;
GO

CREATE PROCEDURE dbo.usp_Announcement_Insert
    @Title NVARCHAR(200),
    @Content NVARCHAR(MAX),
    @IsActive BIT = 1,
    @SortOrder INT = 0,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @CreatedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Announcements (Title, Content, IsActive, SortOrder, StartDate, EndDate, CreatedAt)
    OUTPUT inserted.Id, inserted.Title, inserted.Content, inserted.IsActive, inserted.SortOrder,
           inserted.StartDate, inserted.EndDate, inserted.CreatedAt
    VALUES (@Title, @Content, @IsActive, @SortOrder, @StartDate, @EndDate, @CreatedAt);
END;
GO
