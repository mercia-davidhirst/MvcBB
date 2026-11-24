-- Matches IWordFilterRepository.Add(WordFilterModel filter)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_word_filter_insert(varchar, varchar, boolean, integer);

CREATE PROCEDURE public.sp_word_filter_insert(
    p_word VARCHAR(100),
    p_replacement VARCHAR(100),
    p_is_active BOOLEAN,
    p_sort_order INTEGER,
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
