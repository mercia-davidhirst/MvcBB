-- Matches IPollRepository.GetByThreadId(int threadId)

DROP FUNCTION IF EXISTS public.fn_poll_get_by_thread_id(integer);

CREATE FUNCTION public.fn_poll_get_by_thread_id(p_thread_id INTEGER)
RETURNS SETOF public.polls
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.polls WHERE thread_id = p_thread_id;
$$;
