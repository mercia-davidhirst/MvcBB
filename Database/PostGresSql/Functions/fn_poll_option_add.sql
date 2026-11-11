-- Matches IPollRepository.Add(Poll poll)'s per-option insert
-- (PostgreSqlPollRepository.Add).

DROP FUNCTION IF EXISTS public.fn_poll_option_add(integer, varchar, integer);

CREATE FUNCTION public.fn_poll_option_add(p_poll_id INTEGER, p_text VARCHAR(200), p_sort_order INTEGER DEFAULT 0)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INTEGER;
BEGIN
    INSERT INTO public.poll_options (poll_id, text, sort_order)
    VALUES (p_poll_id, p_text, p_sort_order)
    RETURNING id INTO v_id;
    RETURN v_id;
END;
$$;
