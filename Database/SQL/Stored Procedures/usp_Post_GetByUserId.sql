-- Matches IPostRepository.GetByUserId(int userId, int take) - a member's
-- profile page needs only their few most recent posts, so this filters,
-- sorts and limits entirely in SQL instead of the caller loading every post
-- in the table first (see usp_Post_GetAll).

DROP PROCEDURE IF EXISTS dbo.usp_Post_GetByUserId;
GO

CREATE PROCEDURE dbo.usp_Post_GetByUserId
    @UserId INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take) Id, Content, ThreadId, ThreadTitle, CreatedByUserId, CreatedByUsername,
           CreatedAt, IsEdited, EditReason, UpdatedAt, UpdatedByUserId, UpdatedByUsername,
           IsDeleted, DeletedByUserId, DeletedByUsername, DeletedAt, DeleteReason,
           QuotedPostId, ImageUrl, UserAvatar, UserSignature, UserPostCount, UserJoinedAt, UserRole
    FROM dbo.vw_PostDetails
    WHERE CreatedByUserId = @UserId AND IsDeleted = 0
    ORDER BY CreatedAt DESC;
END;
GO
