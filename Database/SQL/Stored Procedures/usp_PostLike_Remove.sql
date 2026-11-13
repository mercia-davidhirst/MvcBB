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
END;
GO
