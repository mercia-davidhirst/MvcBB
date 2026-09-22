-- Matches IPostRepository.GetAll()

DROP PROCEDURE IF EXISTS dbo.usp_Post_GetAll;
GO

CREATE PROCEDURE dbo.usp_Post_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Content, ThreadId, ThreadTitle, CreatedByUserId, CreatedByUsername,
           CreatedAt, IsEdited, EditReason, UpdatedAt, UpdatedByUserId, UpdatedByUsername,
           IsDeleted, DeletedByUserId, DeletedByUsername, DeletedAt, DeleteReason,
           QuotedPostId, UserAvatar, UserSignature, UserPostCount, UserJoinedAt, UserRole
    FROM dbo.vw_PostDetails
    ORDER BY Id;
END;
GO
