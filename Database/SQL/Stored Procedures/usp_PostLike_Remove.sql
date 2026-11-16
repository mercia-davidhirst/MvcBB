-- Matches IPostLikeRepository.RemoveLike(int postId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_PostLike_Remove;
GO

CREATE PROCEDURE dbo.usp_PostLike_Remove
    @PostId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.PostLikes WHERE PostId = @PostId AND UserId = @UserId;

    IF @@ROWCOUNT > 0
    BEGIN
        UPDATE dbo.Posts SET LikeCount = LikeCount - 1 WHERE Id = @PostId;
        UPDATE dbo.Threads SET TotalLikes = TotalLikes - 1
        WHERE Id = (SELECT ThreadId FROM dbo.Posts WHERE Id = @PostId);
    END;
END;
GO
