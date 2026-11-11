-- Matches IPollRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_poll_get_by_id(integer);

CREATE FUNCTION public.fn_poll_get_by_id(p_id INTEGER)
RETURNS SETOF public.polls
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.polls WHERE id = p_id;
$$;
