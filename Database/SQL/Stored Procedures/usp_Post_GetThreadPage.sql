-- Matches IPostRepository.GetThreadPage(int threadId, int skip, int take)
-- One page of a discussion, in order (CreatedAt, then Id). The page's post
-- ids are picked from dbo.Posts first (IX_Posts_ThreadId_CreatedAt), so the
-- view's per-row work (usernames, the author's post count) is only done for
-- the posts on this page, however long the discussion is. Added in v1.4.0.

DROP PROCEDURE IF EXISTS dbo.usp_Post_GetThreadPage;
GO

CREATE PROCEDURE dbo.usp_Post_GetThreadPage
    @ThreadId INT,
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    WITH pagePosts AS (
        SELECT Id, CreatedAt
        FROM dbo.Posts
        WHERE ThreadId = @ThreadId
        ORDER BY CreatedAt, Id
        OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
    )
    SELECT v.Id, v.Content, v.ThreadId, v.ThreadTitle, v.CreatedByUserId, v.CreatedByUsername,
           v.CreatedAt, v.IsEdited, v.EditReason, v.UpdatedAt, v.UpdatedByUserId, v.UpdatedByUsername,
           v.IsDeleted, v.DeletedByUserId, v.DeletedByUsername, v.DeletedAt, v.DeleteReason,
           v.QuotedPostId, v.ImageUrl, v.IsImportedData, v.UserAvatar, v.UserSignature, v.UserPostCount, v.UserJoinedAt, v.UserRole
    FROM pagePosts AS pp
    INNER JOIN dbo.vw_PostDetails AS v ON v.Id = pp.Id
    ORDER BY pp.CreatedAt, pp.Id;
END;
GO
