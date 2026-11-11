-- Matches IWordFilterRepository.Add(WordFilterModel filter)

DROP PROCEDURE IF EXISTS public.sp_word_filter_insert(varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_word_filter_insert(
    p_word VARCHAR(100),
    p_replacement VARCHAR(100) DEFAULT '',
    p_is_active BOOLEAN DEFAULT TRUE,
    p_sort_order INTEGER DEFAULT 0,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.word_filters (word, replacement, is_active, sort_order)
    VALUES (p_word, p_replacement, p_is_active, p_sort_order)
    RETURNING id INTO p_id;
END;
$$;
