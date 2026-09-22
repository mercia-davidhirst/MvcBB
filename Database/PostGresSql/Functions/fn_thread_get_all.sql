-- Matches IForumThreadRepository.GetAll()

DROP FUNCTION IF EXISTS public.fn_thread_get_all();

CREATE FUNCTION public.fn_thread_get_all()
RETURNS SETOF public.threads
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.threads
    ORDER BY is_sticky DESC, COALESCE(last_post_at, created_at) DESC;
$$;
