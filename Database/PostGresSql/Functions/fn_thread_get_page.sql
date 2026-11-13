-- Matches IForumThreadRepository.GetPage(int skip, int take, bool includePrivateBoards)
-- Filters to visible boards and paginates entirely in SQL, instead of the
-- caller loading every thread in the table into memory first.

DROP FUNCTION IF EXISTS public.fn_thread_get_page(integer, integer, boolean);

CREATE FUNCTION public.fn_thread_get_page(p_skip INTEGER, p_take INTEGER, p_include_private_boards BOOLEAN)
RETURNS SETOF public.vw_thread_details
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.vw_thread_details
    WHERE p_include_private_boards OR NOT board_is_private
    ORDER BY is_sticky DESC, COALESCE(last_post_at, created_at) DESC
    OFFSET p_skip
    LIMIT p_take;
$$;
