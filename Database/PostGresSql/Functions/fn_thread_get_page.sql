-- Matches IForumThreadRepository.GetPage(int skip, int take, bool
-- includePrivateBoards, int? boardId) - filters to visible boards (and
-- optionally one specific board, e.g. a board's own thread listing) and
-- paginates entirely in SQL, instead of the caller loading every thread in
-- the table into memory first.
--
-- Sticky only wins the sort when p_board_id is given (a single board's own
-- thread list, where "pinned to the top of this board" is the intended
-- meaning) - with p_board_id NULL (the home page's site-wide "latest
-- activity across every board" feed), sticky is ignored and everything
-- sorts by recency alone, so an old thread pinned in one board doesn't
-- crowd out genuinely recent activity from every other board.

DROP FUNCTION IF EXISTS public.fn_thread_get_page(integer, integer, boolean);
DROP FUNCTION IF EXISTS public.fn_thread_get_page(integer, integer, boolean, integer);

CREATE FUNCTION public.fn_thread_get_page(p_skip INTEGER, p_take INTEGER, p_include_private_boards BOOLEAN, p_board_id INTEGER DEFAULT NULL)
RETURNS SETOF public.vw_thread_details
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.vw_thread_details
    WHERE (p_include_private_boards OR NOT board_is_private)
      AND (p_board_id IS NULL OR board_id = p_board_id)
    ORDER BY (p_board_id IS NOT NULL AND is_sticky) DESC, COALESCE(last_post_at, created_at) DESC
    OFFSET p_skip
    LIMIT p_take;
$$;
