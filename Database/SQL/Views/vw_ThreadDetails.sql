/*
    View: dbo.vw_ThreadDetails
    Purpose: Reconstructs the full MvcBB.Shared.Models.ForumThread.ForumThread
             shape, including the presentation fields that IForumThreadRepository's
             GetAll/GetByBoardId/GetById/GetPage results carry alongside the raw
             row (BoardName, BoardIsPrivate, CreatedByUsername, LastPostByUsername) -
             matches the same pattern vw_PostDetails already uses for Post. Used by
             usp_Thread_GetAll, usp_Thread_GetByBoardId, usp_Thread_GetById,
             usp_Thread_GetPage and usp_Thread_GetPageCount.
*/

DROP VIEW IF EXISTS dbo.vw_ThreadDetails;
GO

CREATE VIEW dbo.vw_ThreadDetails
AS
SELECT
    t.Id,
    t.Title,
    t.BoardId,
    b.Name                     AS BoardName,
    b.IsPrivate                AS BoardIsPrivate,
    t.CreatedByUserId,
    creator.Username            AS CreatedByUsername,
    t.CreatedAt,
    t.UpdatedAt,
    t.IsSticky,
    t.IsLocked,
    t.ViewCount,
    t.PostCount,
    t.LastPostAt,
    t.LastPostByUserId,
    lastPoster.Username         AS LastPostByUsername
FROM dbo.Threads AS t
INNER JOIN dbo.Boards AS b ON b.Id = t.BoardId
INNER JOIN dbo.Users AS creator ON creator.Id = t.CreatedByUserId
LEFT JOIN dbo.Users AS lastPoster ON lastPoster.Id = t.LastPostByUserId;
GO
