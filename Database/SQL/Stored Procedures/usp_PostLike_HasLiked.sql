-- Matches IPostLikeRepository.HasLiked(int postId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_PostLike_HasLiked;
GO

CREATE PROCEDURE dbo.usp_PostLike_HasLiked
    @PostId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN EXISTS (
        SELECT 1 FROM dbo.PostLikes WHERE PostId = @PostId AND UserId = @UserId
    ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
END;
GO
