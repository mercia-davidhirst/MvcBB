-- Matches IForumThreadRepository.Search's total count (the full filtered
-- count, not just the page returned) - a separate call rather than a window
-- function so it stays correct even when OFFSET skips past every row, same
-- reasoning as fn_thread_get_page_count.

DROP FUNCTION IF EXISTS public.fn_search_threads_count(text, integer, boolean);

CREATE FUNCTION public.fn_search_threads_count(
    p_query TEXT,
    p_board_id INTEGER,
    p_include_private_boards BOOLEAN
)
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)
    FROM public.vw_thread_details
    WHERE (title ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
           OR created_by_username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\')
      AND (p_board_id IS NULL OR board_id = p_board_id)
      AND (p_include_private_boards OR NOT board_is_private);
$$;
