-- Matches IForumThreadRepository.Search(string query, int? boardId, bool
-- includePrivateBoards, int skip, int take) - filters by title/author
-- username, an optional board and board visibility, then paginates entirely
-- in SQL instead of the caller loading every thread in the table first.

DROP PROCEDURE IF EXISTS dbo.usp_Search_Threads;
GO

CREATE PROCEDURE dbo.usp_Search_Threads
    @Query NVARCHAR(100),
    @BoardId INT = NULL,
    @IncludePrivateBoards BIT,
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Escape LIKE wildcards in the user-supplied query, and lower-case both
    -- sides regardless of the database's collation, so this matches the same
    -- substrings as the C# string.ToLower().Contains() it replaces.
    DECLARE @Pattern NVARCHAR(MAX) = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Query), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    SELECT Id, Title, BoardId, BoardName, BoardIsPrivate, CreatedByUserId, CreatedByUsername,
           CreatedAt, UpdatedAt, IsSticky, IsLocked, ViewCount, PostCount, LastPostAt,
           LastPostByUserId, LastPostByUsername
    FROM dbo.vw_ThreadDetails
    WHERE (LOWER(Title) LIKE @Pattern OR LOWER(CreatedByUsername) LIKE @Pattern)
      AND (@BoardId IS NULL OR BoardId = @BoardId)
      AND (@IncludePrivateBoards = 1 OR BoardIsPrivate = 0)
    ORDER BY CreatedAt DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
