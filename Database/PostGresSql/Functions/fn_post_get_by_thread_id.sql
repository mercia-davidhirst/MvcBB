-- Matches IPostRepository.GetByThreadId(int threadId)
-- Depends on public.vw_post_details (Views/vw_post_details.sql must be
-- deployed first).

DROP FUNCTION IF EXISTS public.fn_post_get_by_thread_id(integer);

CREATE FUNCTION public.fn_post_get_by_thread_id(p_thread_id INTEGER)
RETURNS SETOF public.vw_post_details
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.vw_post_details
    WHERE thread_id = p_thread_id
    ORDER BY created_at;
$$;
