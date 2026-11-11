-- Matches IRankRepository.GetAll() (ordered by sort_order)

DROP FUNCTION IF EXISTS public.fn_rank_get_all();

CREATE FUNCTION public.fn_rank_get_all()
RETURNS SETOF public.ranks
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.ranks ORDER BY sort_order;
$$;
