-- Matches IForumThreadRepository.GetByBoardId(int boardId)
-- Ordering matches InMemoryForumThreadRepository: sticky threads first, then
-- most recently active.

DROP FUNCTION IF EXISTS public.fn_thread_get_by_board_id(integer);

CREATE FUNCTION public.fn_thread_get_by_board_id(p_board_id INTEGER)
RETURNS SETOF public.threads
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.threads
    WHERE board_id = p_board_id
    ORDER BY is_sticky DESC, COALESCE(last_post_at, created_at) DESC;
$$;
