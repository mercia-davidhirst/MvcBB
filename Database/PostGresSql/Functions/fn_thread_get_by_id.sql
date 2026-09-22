-- Matches IForumThreadRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_thread_get_by_id(integer);

CREATE FUNCTION public.fn_thread_get_by_id(p_id INTEGER)
RETURNS SETOF public.threads
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.threads WHERE id = p_id;
$$;
