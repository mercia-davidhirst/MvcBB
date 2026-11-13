-- Matches IForumThreadRepository.GetPage(int skip, int take, bool includePrivateBoards)
-- Filters to visible boards and paginates entirely in SQL, instead of the
-- caller loading every thread in the table into memory first.

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetPage;
GO

CREATE PROCEDURE dbo.usp_Thread_GetPage
    @Skip INT,
    @Take INT,
    @IncludePrivateBoards BIT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, BoardId, BoardName, BoardIsPrivate, CreatedByUserId, CreatedByUsername,
           CreatedAt, UpdatedAt, IsSticky, IsLocked, ViewCount, PostCount, LastPostAt,
           LastPostByUserId, LastPostByUsername
    FROM dbo.vw_ThreadDetails
    WHERE @IncludePrivateBoards = 1 OR BoardIsPrivate = 0
    ORDER BY IsSticky DESC, ISNULL(LastPostAt, CreatedAt) DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
