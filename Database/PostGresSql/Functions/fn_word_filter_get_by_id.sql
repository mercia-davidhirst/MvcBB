-- Matches IWordFilterRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_word_filter_get_by_id(integer);

CREATE FUNCTION public.fn_word_filter_get_by_id(p_id INTEGER)
RETURNS SETOF public.word_filters
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.word_filters WHERE id = p_id;
$$;
