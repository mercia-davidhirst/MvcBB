-- Matches IRankRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_rank_get_by_id(integer);

CREATE FUNCTION public.fn_rank_get_by_id(p_id INTEGER)
RETURNS SETOF public.ranks
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.ranks WHERE id = p_id;
$$;
