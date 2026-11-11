-- Matches IWordFilterRepository.Update(WordFilterModel filter)

DROP PROCEDURE IF EXISTS public.sp_word_filter_update(integer, varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_word_filter_update(
    p_id INTEGER,
    p_word VARCHAR(100),
    p_replacement VARCHAR(100),
    p_is_active BOOLEAN,
    p_sort_order INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.word_filters
    SET word = p_word,
        replacement = p_replacement,
        is_active = p_is_active,
        sort_order = p_sort_order
    WHERE id = p_id;
END;
$$;
