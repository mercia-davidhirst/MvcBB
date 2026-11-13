-- Matches IForumThreadRepository.GetPage's total count (the full filtered
-- count, not just the page returned) - a separate call rather than a window
-- function so it stays correct even when OFFSET skips past every row.

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetPageCount;
GO

CREATE PROCEDURE dbo.usp_Thread_GetPageCount
    @IncludePrivateBoards BIT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*)
    FROM dbo.vw_ThreadDetails
    WHERE @IncludePrivateBoards = 1 OR BoardIsPrivate = 0;
END;
GO
