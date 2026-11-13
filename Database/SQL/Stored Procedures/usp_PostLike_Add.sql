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
    END;
END;
GO
