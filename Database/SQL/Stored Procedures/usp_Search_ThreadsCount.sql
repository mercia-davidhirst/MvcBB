-- Matches IForumThreadRepository.Search's total count (the full filtered
-- count, not just the page returned) - a separate call rather than a window
-- function so it stays correct even when OFFSET skips past every row, same
-- reasoning as usp_Thread_GetPageCount.

DROP PROCEDURE IF EXISTS dbo.usp_Search_ThreadsCount;
GO

CREATE PROCEDURE dbo.usp_Search_ThreadsCount
    @Query NVARCHAR(100),
    @BoardId INT = NULL,
    @IncludePrivateBoards BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(MAX) = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Query), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT COUNT(*)
    FROM dbo.vw_ThreadDetails
    WHERE (LOWER(Title) LIKE @Pattern OR LOWER(CreatedByUsername) LIKE @Pattern)
      AND (@BoardId IS NULL OR BoardId = @BoardId)
      AND (@IncludePrivateBoards = 1 OR BoardIsPrivate = 0);
END;
GO
