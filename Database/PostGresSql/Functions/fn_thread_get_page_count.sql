-- Matches IForumThreadRepository.GetPage's total count (the full filtered
-- count, not just the page returned) - a separate call rather than a window
-- function so it stays correct even when OFFSET skips past every row.

DROP FUNCTION IF EXISTS public.fn_thread_get_page_count(boolean);
DROP FUNCTION IF EXISTS public.fn_thread_get_page_count(boolean, integer);

CREATE FUNCTION public.fn_thread_get_page_count(p_include_private_boards BOOLEAN, p_board_id INTEGER DEFAULT NULL)
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*) FROM public.vw_thread_details
    WHERE (p_include_private_boards OR NOT board_is_private)
      AND (p_board_id IS NULL OR board_id = p_board_id);
$$;
