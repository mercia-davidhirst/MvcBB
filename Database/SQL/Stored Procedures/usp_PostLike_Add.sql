-- Matches IPostLikeRepository.AddLike(int postId, string userId)
-- A no-op if the like already exists, matching the repositories' semantics.

DROP PROCEDURE IF EXISTS dbo.usp_PostLike_Add;
GO

CREATE PROCEDURE dbo.usp_PostLike_Add
    @PostId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.PostLikes WHERE PostId = @PostId AND UserId = @UserId)
    BEGIN
        INSERT INTO dbo.PostLikes (PostId, UserId)
        VALUES (@PostId, @UserId);

        -- Posts.LikeCount/Threads.TotalLikes are maintained here rather than
        -- recomputed with COUNT(*) on every read - this proc is the only
        -- place a like is ever added, so it's the only place that needs to
        -- keep them in sync.
        UPDATE dbo.Posts SET LikeCount = LikeCount + 1 WHERE Id = @PostId;
        UPDATE dbo.Threads SET TotalLikes = TotalLikes + 1
        WHERE Id = (SELECT ThreadId FROM dbo.Posts WHERE Id = @PostId);
    END;
END;
GO
