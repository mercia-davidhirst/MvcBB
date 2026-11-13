-- Matches IPostRepository.Search's total count (the full filtered count,
-- not just the page returned) - a separate call rather than a window
-- function, same reasoning as fn_thread_get_page_count.

DROP FUNCTION IF EXISTS public.fn_search_posts_count(text, integer, boolean);

CREATE FUNCTION public.fn_search_posts_count(
    p_query TEXT,
    p_board_id INTEGER,
    p_include_private_boards BOOLEAN
)
RETURNS BIGINT
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)
    FROM public.vw_post_details AS pd
    INNER JOIN public.threads AS th ON th.id = pd.thread_id
    INNER JOIN public.boards AS b ON b.id = th.board_id
    WHERE (pd.content ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
           OR pd.created_by_username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\')
      AND (p_board_id IS NULL OR b.id = p_board_id)
      AND (p_include_private_boards OR NOT b.is_private);
$$;
