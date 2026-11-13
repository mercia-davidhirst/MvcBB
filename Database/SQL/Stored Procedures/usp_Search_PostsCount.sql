-- Matches IPostRepository.Search's total count (the full filtered count,
-- not just the page returned) - a separate call rather than a window
-- function, same reasoning as usp_Thread_GetPageCount.

DROP PROCEDURE IF EXISTS dbo.usp_Search_PostsCount;
GO

CREATE PROCEDURE dbo.usp_Search_PostsCount
    @Query NVARCHAR(100),
    @BoardId INT = NULL,
    @IncludePrivateBoards BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(MAX) = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Query), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT COUNT(*)
    FROM dbo.vw_PostDetails AS pd
    INNER JOIN dbo.Threads AS th ON th.Id = pd.ThreadId
    INNER JOIN dbo.Boards AS b ON b.Id = th.BoardId
    WHERE (LOWER(pd.Content) LIKE @Pattern OR LOWER(pd.CreatedByUsername) LIKE @Pattern)
      AND (@BoardId IS NULL OR b.Id = @BoardId)
      AND (@IncludePrivateBoards = 1 OR b.IsPrivate = 0);
END;
GO
