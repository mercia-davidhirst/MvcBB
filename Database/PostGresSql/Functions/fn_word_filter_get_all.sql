-- Matches IWordFilterRepository.GetAll() (ordered by sort_order)

DROP FUNCTION IF EXISTS public.fn_word_filter_get_all();

CREATE FUNCTION public.fn_word_filter_get_all()
RETURNS SETOF public.word_filters
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.word_filters ORDER BY sort_order;
$$;
