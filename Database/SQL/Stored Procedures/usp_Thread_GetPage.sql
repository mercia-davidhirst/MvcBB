-- Matches IForumThreadRepository.GetPage(int skip, int take, bool
-- includePrivateBoards, int? boardId) - filters to visible boards (and
-- optionally one specific board, e.g. a board's own thread listing) and
-- paginates entirely in SQL, instead of the caller loading every thread in
-- the table into memory first.
--
-- Sticky only wins the sort when @BoardId is given (a single board's own
-- thread list, where "pinned to the top of this board" is the intended
-- meaning) - with @BoardId NULL (the home page's site-wide "latest activity
-- across every board" feed), sticky is ignored and everything sorts by
-- recency alone, so an old thread pinned in one board doesn't crowd out
-- genuinely recent activity from every other board.

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetPage;
GO

CREATE PROCEDURE dbo.usp_Thread_GetPage
    @Skip INT,
    @Take INT,
    @IncludePrivateBoards BIT,
    @BoardId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, BoardId, BoardName, BoardIsPrivate, CreatedByUserId, CreatedByUsername,
           CreatedAt, UpdatedAt, IsSticky, IsLocked, ViewCount, PostCount, TotalLikes,
           ParticipantCount, LastPostAt, LastPostByUserId, LastPostByUsername, IsImportedData
    FROM dbo.vw_ThreadDetails
    WHERE (@IncludePrivateBoards = 1 OR BoardIsPrivate = 0)
      AND (@BoardId IS NULL OR BoardId = @BoardId)
    ORDER BY CASE WHEN @BoardId IS NOT NULL AND IsSticky = 1 THEN 1 ELSE 0 END DESC,
             ISNULL(LastPostAt, CreatedAt) DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
