-- Matches IUserRepository.GetMembersPage(UserRole? role, string? search,
-- string? sortBy, bool sortDescending, int skip, int take) - filters by
-- role/username, sorts (including by post/thread count, via a correlated
-- subquery) and paginates entirely in SQL instead of the caller loading
-- every user in the table first.
--
-- The CASE-per-sort-option ORDER BY is the standard trick for a dynamic
-- sort column without dynamic SQL: for any given @SortBy only one of these
-- CASE expressions is non-NULL for every row, so the others contribute no
-- tie-break ordering at all.

DROP PROCEDURE IF EXISTS dbo.usp_User_GetMembersPage;
GO

CREATE PROCEDURE dbo.usp_User_GetMembersPage
    @Role TINYINT = NULL,
    @Search NVARCHAR(50) = NULL,
    @SortBy NVARCHAR(20) = NULL,
    @SortDescending BIT = 0,
    @Skip INT,
    @Take INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(MAX) = NULL;
    IF @Search IS NOT NULL AND LEN(@Search) > 0
        SET @Pattern = '%' + REPLACE(REPLACE(REPLACE(LOWER(@Search), '[', '[[]'), '%', '[%]'), '_', '[_]') + '%';

    DECLARE @NormalizedSortBy NVARCHAR(20) = LOWER(@SortBy);

    SELECT u.Id, u.Username, u.PasswordHash, u.Email, u.CreatedAt, u.LastLoginAt, u.Role,
           u.Signature, u.Bio, u.AvatarUrl, u.ShowEmail, u.CustomTitle,
           u.MustChangePassword, u.LockedUntil, u.LastLoginIp
    FROM dbo.Users AS u
    WHERE (@Role IS NULL OR u.Role = @Role)
      AND (@Pattern IS NULL OR LOWER(u.Username) LIKE @Pattern)
    ORDER BY
        CASE WHEN @NormalizedSortBy = 'joined' AND @SortDescending = 0 THEN u.CreatedAt END ASC,
        CASE WHEN @NormalizedSortBy = 'joined' AND @SortDescending = 1 THEN u.CreatedAt END DESC,
        CASE WHEN @NormalizedSortBy = 'posts' AND @SortDescending = 0
             THEN (SELECT COUNT(*) FROM dbo.Posts p WHERE p.CreatedByUserId = u.Id AND p.IsDeleted = 0) END ASC,
        CASE WHEN @NormalizedSortBy = 'posts' AND @SortDescending = 1
             THEN (SELECT COUNT(*) FROM dbo.Posts p WHERE p.CreatedByUserId = u.Id AND p.IsDeleted = 0) END DESC,
        CASE WHEN @NormalizedSortBy = 'threads' AND @SortDescending = 0
             THEN (SELECT COUNT(*) FROM dbo.Threads t WHERE t.CreatedByUserId = u.Id) END ASC,
        CASE WHEN @NormalizedSortBy = 'threads' AND @SortDescending = 1
             THEN (SELECT COUNT(*) FROM dbo.Threads t WHERE t.CreatedByUserId = u.Id) END DESC,
        CASE WHEN @NormalizedSortBy = 'username' AND @SortDescending = 1 THEN u.Username END DESC,
        CASE WHEN @NormalizedSortBy NOT IN ('joined', 'posts', 'threads')
                  AND NOT (@NormalizedSortBy = 'username' AND @SortDescending = 1)
             THEN u.Username END ASC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END;
GO
