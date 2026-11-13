-- Matches IForumThreadRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_thread_get_by_id(integer);

CREATE FUNCTION public.fn_thread_get_by_id(p_id INTEGER)
RETURNS SETOF public.vw_thread_details
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.vw_thread_details WHERE id = p_id;
$$;
