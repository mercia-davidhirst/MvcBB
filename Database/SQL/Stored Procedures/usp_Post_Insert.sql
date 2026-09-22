-- Matches IPostRepository.Add(Post post)
-- Only base Posts columns are accepted/returned; the joined presentation
-- fields (ThreadTitle, CreatedByUsername, etc.) are read back via
-- usp_Post_GetById / dbo.vw_PostDetails after insert.

DROP PROCEDURE IF EXISTS dbo.usp_Post_Insert;
GO

CREATE PROCEDURE dbo.usp_Post_Insert
    @Content NVARCHAR(MAX),
    @ThreadId INT,
    @CreatedByUserId INT,
    @CreatedAt DATETIME2(3) = NULL,
    @QuotedPostId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Posts (Content, ThreadId, CreatedByUserId, CreatedAt, QuotedPostId)
    OUTPUT inserted.Id
    VALUES (@Content, @ThreadId, @CreatedByUserId, ISNULL(@CreatedAt, SYSUTCDATETIME()), @QuotedPostId);
END;
GO
