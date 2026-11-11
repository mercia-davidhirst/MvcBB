-- Matches IWordFilterRepository.Remove(WordFilterModel filter)

DROP PROCEDURE IF EXISTS public.sp_word_filter_delete(integer);

CREATE PROCEDURE public.sp_word_filter_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.word_filters WHERE id = p_id;
END;
$$;
