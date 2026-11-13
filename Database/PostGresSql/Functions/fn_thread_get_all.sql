-- Matches IForumThreadRepository.GetAll()

DROP FUNCTION IF EXISTS public.fn_thread_get_all();

CREATE FUNCTION public.fn_thread_get_all()
RETURNS SETOF public.vw_thread_details
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.vw_thread_details
    ORDER BY is_sticky DESC, COALESCE(last_post_at, created_at) DESC;
$$;
