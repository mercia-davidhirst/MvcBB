-- Matches IPostRepository.Search(string query, int? boardId, bool
-- includePrivateBoards, int skip, int take) - filters by content/author
-- username, an optional board (via each post's thread) and board
-- visibility, then paginates entirely in SQL. The Threads/Boards join exists
-- only to filter; the returned columns match vw_PostDetails exactly, same
-- shape as the other usp_Post_Get* procedures - board id/name for display
-- come from a small batched IForumThreadRepository.GetByIds call afterwards,
-- since ForumThread already carries them denormalized.

DROP PROCEDURE IF EXISTS dbo.usp_Search_Posts;
GO

CREATE PROCEDURE dbo.usp_Search_Posts
    @Query NVARCHAR(100),
    @BoardId INT = NULL,
    @IncludePrivateBoards BIT,
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(MAX) = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Query), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT pd.Id, pd.Content, pd.ThreadId, pd.ThreadTitle, pd.CreatedByUserId, pd.CreatedByUsername,
           pd.CreatedAt, pd.IsEdited, pd.EditReason, pd.UpdatedAt, pd.UpdatedByUserId, pd.UpdatedByUsername,
           pd.IsDeleted, pd.DeletedByUserId, pd.DeletedByUsername, pd.DeletedAt, pd.DeleteReason,
           pd.QuotedPostId, pd.ImageUrl, pd.UserAvatar, pd.UserSignature, pd.UserPostCount, pd.UserJoinedAt, pd.UserRole
    FROM dbo.vw_PostDetails AS pd
    INNER JOIN dbo.Threads AS th ON th.Id = pd.ThreadId
    INNER JOIN dbo.Boards AS b ON b.Id = th.BoardId
    WHERE (LOWER(pd.Content) LIKE @Pattern OR LOWER(pd.CreatedByUsername) LIKE @Pattern)
      AND (@BoardId IS NULL OR b.Id = @BoardId)
      AND (@IncludePrivateBoards = 1 OR b.IsPrivate = 0)
    ORDER BY pd.CreatedAt DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
