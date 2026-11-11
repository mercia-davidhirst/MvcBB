-- Matches IAnnouncementManagementService.UpdateAnnouncement(int id, AnnouncementModel model)

DROP PROCEDURE IF EXISTS dbo.usp_Announcement_Update;
GO

CREATE PROCEDURE dbo.usp_Announcement_Update
    @Id INT,
    @Title NVARCHAR(200),
    @Content NVARCHAR(MAX),
    @IsActive BIT,
    @SortOrder INT,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Announcements
    SET Title = @Title,
        Content = @Content,
        IsActive = @IsActive,
        SortOrder = @SortOrder,
        StartDate = @StartDate,
        EndDate = @EndDate
    WHERE Id = @Id;
END;
GO
