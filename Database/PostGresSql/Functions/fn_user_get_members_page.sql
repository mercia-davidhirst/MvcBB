-- Matches IUserRepository.GetMembersPage(UserRole? role, string? search,
-- string? sortBy, bool sortDescending, int skip, int take) - filters by
-- role/username, sorts (including by post/thread count, via a correlated
-- subquery) and paginates entirely in SQL instead of the caller loading
-- every user in the table first.
--
-- The CASE-per-sort-option ORDER BY is the standard trick for a dynamic
-- sort column without dynamic SQL: for any given sort option only one of
-- these CASE expressions is non-NULL for every row, so the others
-- contribute no tie-break ordering at all.

DROP FUNCTION IF EXISTS public.fn_user_get_members_page(smallint, text, text, boolean, integer, integer);

CREATE FUNCTION public.fn_user_get_members_page(
    p_role SMALLINT,
    p_search TEXT,
    p_sort_by TEXT,
    p_sort_descending BOOLEAN,
    p_skip INTEGER,
    p_take INTEGER
)
RETURNS SETOF public.users
LANGUAGE sql
STABLE
AS $$
    SELECT u.*
    FROM public.users AS u
    WHERE (p_role IS NULL OR u.role = p_role)
      AND (p_search IS NULL OR p_search = ''
           OR u.username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_search, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\')
    ORDER BY
        CASE WHEN LOWER(p_sort_by) = 'joined' AND NOT p_sort_descending THEN u.created_at END ASC NULLS LAST,
        CASE WHEN LOWER(p_sort_by) = 'joined' AND p_sort_descending THEN u.created_at END DESC NULLS LAST,
        CASE WHEN LOWER(p_sort_by) = 'posts' AND NOT p_sort_descending
             THEN (SELECT COUNT(*) FROM public.posts p WHERE p.created_by_user_id = u.id AND p.is_deleted = FALSE) END ASC NULLS LAST,
        CASE WHEN LOWER(p_sort_by) = 'posts' AND p_sort_descending
             THEN (SELECT COUNT(*) FROM public.posts p WHERE p.created_by_user_id = u.id AND p.is_deleted = FALSE) END DESC NULLS LAST,
        CASE WHEN LOWER(p_sort_by) = 'threads' AND NOT p_sort_descending
             THEN (SELECT COUNT(*) FROM public.threads t WHERE t.created_by_user_id = u.id) END ASC NULLS LAST,
        CASE WHEN LOWER(p_sort_by) = 'threads' AND p_sort_descending
             THEN (SELECT COUNT(*) FROM public.threads t WHERE t.created_by_user_id = u.id) END DESC NULLS LAST,
        CASE WHEN LOWER(p_sort_by) = 'username' AND p_sort_descending THEN u.username END DESC NULLS LAST,
        CASE WHEN COALESCE(LOWER(p_sort_by), '') NOT IN ('joined', 'posts', 'threads')
                  AND NOT (LOWER(p_sort_by) = 'username' AND p_sort_descending)
             THEN u.username END ASC NULLS LAST
    OFFSET p_skip
    LIMIT p_take;
$$;
