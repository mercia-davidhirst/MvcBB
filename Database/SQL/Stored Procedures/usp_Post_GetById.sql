-- Matches IPostRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Post_GetById;
GO

CREATE PROCEDURE dbo.usp_Post_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Content, ThreadId, ThreadTitle, CreatedByUserId, CreatedByUsername,
           CreatedAt, IsEdited, EditReason, UpdatedAt, UpdatedByUserId, UpdatedByUsername,
           IsDeleted, DeletedByUserId, DeletedByUsername, DeletedAt, DeleteReason,
           QuotedPostId, UserAvatar, UserSignature, UserPostCount, UserJoinedAt, UserRole
    FROM dbo.vw_PostDetails
    WHERE Id = @Id;
END;
GO
