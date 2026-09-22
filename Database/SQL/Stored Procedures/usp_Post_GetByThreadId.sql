-- Matches IPostRepository.GetByThreadId(int threadId)

DROP PROCEDURE IF EXISTS dbo.usp_Post_GetByThreadId;
GO

CREATE PROCEDURE dbo.usp_Post_GetByThreadId
    @ThreadId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Content, ThreadId, ThreadTitle, CreatedByUserId, CreatedByUsername,
           CreatedAt, IsEdited, EditReason, UpdatedAt, UpdatedByUserId, UpdatedByUsername,
           IsDeleted, DeletedByUserId, DeletedByUsername, DeletedAt, DeleteReason,
           QuotedPostId, UserAvatar, UserSignature, UserPostCount, UserJoinedAt, UserRole
    FROM dbo.vw_PostDetails
    WHERE ThreadId = @ThreadId
    ORDER BY CreatedAt;
END;
GO
