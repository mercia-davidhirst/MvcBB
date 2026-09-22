/*
    View: dbo.vw_PostDetails
    Purpose: Reconstructs the full MvcBB.Shared.Models.Post.Post shape,
             including the presentation fields that IPostRepository's
             GetAll/GetByThreadId/GetById results carry alongside the raw row
             (ThreadTitle, CreatedByUsername, UpdatedByUsername,
             DeletedByUsername, UserAvatar, UserSignature, UserPostCount,
             UserJoinedAt, UserRole). Used by usp_Post_GetAll,
             usp_Post_GetByThreadId and usp_Post_GetById.
*/

DROP VIEW IF EXISTS dbo.vw_PostDetails;
GO

CREATE VIEW dbo.vw_PostDetails
AS
SELECT
    p.Id,
    p.Content,
    p.ThreadId,
    t.Title                            AS ThreadTitle,
    p.CreatedByUserId,
    creator.Username                   AS CreatedByUsername,
    p.CreatedAt,
    p.IsEdited,
    p.EditReason,
    p.UpdatedAt,
    p.UpdatedByUserId,
    updater.Username                   AS UpdatedByUsername,
    p.IsDeleted,
    p.DeletedByUserId,
    deleter.Username                   AS DeletedByUsername,
    p.DeletedAt,
    p.DeleteReason,
    p.QuotedPostId,
    creator.AvatarUrl                  AS UserAvatar,
    creator.Signature                  AS UserSignature,
    postCounts.UserPostCount,
    creator.CreatedAt                  AS UserJoinedAt,
    dbo.ufn_RoleName(creator.Role)      AS UserRole
FROM dbo.Posts AS p
INNER JOIN dbo.Threads AS t ON t.Id = p.ThreadId
INNER JOIN dbo.Users AS creator ON creator.Id = p.CreatedByUserId
LEFT JOIN dbo.Users AS updater ON updater.Id = p.UpdatedByUserId
LEFT JOIN dbo.Users AS deleter ON deleter.Id = p.DeletedByUserId
CROSS APPLY (
    SELECT COUNT(*) AS UserPostCount
    FROM dbo.Posts AS cp
    WHERE cp.CreatedByUserId = p.CreatedByUserId
      AND cp.IsDeleted = 0
) AS postCounts;
GO
