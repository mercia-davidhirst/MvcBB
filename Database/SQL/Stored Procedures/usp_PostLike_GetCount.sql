-- Matches IPostLikeRepository.GetLikeCount(int postId)

DROP PROCEDURE IF EXISTS dbo.usp_PostLike_GetCount;
GO

CREATE PROCEDURE dbo.usp_PostLike_GetCount
    @PostId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.PostLikes WHERE PostId = @PostId;
END;
GO
