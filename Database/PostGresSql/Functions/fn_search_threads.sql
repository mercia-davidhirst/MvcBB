-- Matches IForumThreadRepository.Search(string query, int? boardId, bool
-- includePrivateBoards, int skip, int take) - filters by title/author
-- username, an optional board and board visibility, then paginates entirely
-- in SQL instead of the caller loading every thread in the table first.

DROP FUNCTION IF EXISTS public.fn_search_threads(text, integer, boolean, integer, integer);

CREATE FUNCTION public.fn_search_threads(
    p_query TEXT,
    p_board_id INTEGER,
    p_include_private_boards BOOLEAN,
    p_skip INTEGER,
    p_take INTEGER
)
RETURNS SETOF public.vw_thread_details
LANGUAGE sql
STABLE
AS $$
    -- ILIKE is already case-insensitive regardless of column type; escaping
    -- %/_/\ keeps a literal occurrence in the query from acting as a
    -- wildcard, matching the C# string.Contains() this replaces.
    SELECT *
    FROM public.vw_thread_details
    WHERE (title ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
           OR created_by_username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\')
      AND (p_board_id IS NULL OR board_id = p_board_id)
      AND (p_include_private_boards OR NOT board_is_private)
    ORDER BY created_at DESC
    OFFSET p_skip
    LIMIT p_take;
$$;
