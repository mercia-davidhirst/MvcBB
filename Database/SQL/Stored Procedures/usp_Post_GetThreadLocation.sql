-- Matches IPostRepository.GetThreadLocation(int threadId, int? postId, DateTime? unreadAfter, int? excludeUserId)
-- Returns the discussion's post count and, for @PostId (or, without it, the
-- first post after @UnreadAfter not written by @ExcludeUserId), that post's id
-- and 0-based position in (CreatedAt, Id) order - so the App can open the
-- page it's on. PostId/Position are NULL when there's no such post.
-- Added in v1.4.0.

DROP PROCEDURE IF EXISTS dbo.usp_Post_GetThreadLocation;
GO

CREATE PROCEDURE dbo.usp_Post_GetThreadLocation
    @ThreadId INT,
    @PostId INT = NULL,
    @UnreadAfter DATETIME2(3) = NULL,
    @ExcludeUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalCount INT = (SELECT COUNT(*) FROM dbo.Posts WHERE ThreadId = @ThreadId);
    DECLARE @TargetId INT = NULL;
    DECLARE @TargetAt DATETIME2(3) = NULL;

    IF @PostId IS NOT NULL
    BEGIN
        SELECT @TargetId = Id, @TargetAt = CreatedAt
        FROM dbo.Posts
        WHERE Id = @PostId AND ThreadId = @ThreadId;
    END
    ELSE IF @UnreadAfter IS NOT NULL
    BEGIN
        SELECT TOP (1) @TargetId = Id, @TargetAt = CreatedAt
        FROM dbo.Posts
        WHERE ThreadId = @ThreadId
          AND CreatedAt > @UnreadAfter
          AND (@ExcludeUserId IS NULL OR CreatedByUserId <> @ExcludeUserId)
        ORDER BY CreatedAt, Id;
    END

    SELECT @TotalCount AS TotalCount,
           @TargetId AS PostId,
           CASE WHEN @TargetId IS NULL THEN NULL ELSE (
               SELECT COUNT(*)
               FROM dbo.Posts
               WHERE ThreadId = @ThreadId
                 AND (CreatedAt < @TargetAt OR (CreatedAt = @TargetAt AND Id < @TargetId))
           ) END AS Position;
END;
GO
