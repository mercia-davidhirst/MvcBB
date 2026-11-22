-- Matches IPostRepository.GetByUserId(int userId, int take, bool
-- includePrivateBoards, bool includeInactiveBoards) - a member's profile
-- page needs only their few most recent posts, so this filters, sorts and
-- limits entirely in SQL instead of the caller loading every post in the
-- table first (see usp_Post_GetAll). The Threads/Boards join applies the
-- same board rules as the rest of the site: guests never get posts from
-- private boards, and only administrators get posts from inactive boards.

DROP PROCEDURE IF EXISTS dbo.usp_Post_GetByUserId;
GO

CREATE PROCEDURE dbo.usp_Post_GetByUserId
    @UserId INT,
    @Take INT,
    @IncludePrivateBoards BIT,
    @IncludeInactiveBoards BIT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) pd.Id, pd.Content, pd.ThreadId, pd.ThreadTitle, pd.CreatedByUserId, pd.CreatedByUsername,
           pd.CreatedAt, pd.IsEdited, pd.EditReason, pd.UpdatedAt, pd.UpdatedByUserId, pd.UpdatedByUsername,
           pd.IsDeleted, pd.DeletedByUserId, pd.DeletedByUsername, pd.DeletedAt, pd.DeleteReason,
           pd.QuotedPostId, pd.ImageUrl, pd.IsImportedData, pd.UserAvatar, pd.UserSignature, pd.UserPostCount, pd.UserJoinedAt, pd.UserRole
    FROM dbo.vw_PostDetails AS pd
    INNER JOIN dbo.Threads AS th ON th.Id = pd.ThreadId
    INNER JOIN dbo.Boards AS b ON b.Id = th.BoardId
    WHERE pd.CreatedByUserId = @UserId
      AND pd.IsDeleted = 0
      AND (@IncludePrivateBoards = 1 OR b.IsPrivate = 0)
      AND (@IncludeInactiveBoards = 1 OR b.IsActive = 1)
    ORDER BY pd.CreatedAt DESC;
END;
GO
