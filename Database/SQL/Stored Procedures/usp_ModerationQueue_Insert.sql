-- Matches IModerationQueueRepository.Add(ModerationQueueItem item)

DROP PROCEDURE IF EXISTS dbo.usp_ModerationQueue_Insert;
GO

CREATE PROCEDURE dbo.usp_ModerationQueue_Insert
    @Kind TINYINT,
    @UserId INT,
    @BoardId INT,
    @ThreadId INT = NULL,
    @Title NVARCHAR(200) = NULL,
    @Content NVARCHAR(MAX),
    @QuotedPostId INT = NULL,
    @ImageUrl NVARCHAR(500) = NULL,
    @PollJson NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ModerationQueue (Kind, UserId, BoardId, ThreadId, Title, Content, QuotedPostId, ImageUrl, PollJson)
    OUTPUT inserted.Id
    VALUES (@Kind, @UserId, @BoardId, @ThreadId, @Title, @Content, @QuotedPostId, @ImageUrl, @PollJson);
END;
GO
