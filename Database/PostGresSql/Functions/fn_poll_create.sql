-- Matches IPollRepository.Add(Poll poll) - inserts the poll row only; options
-- are added one at a time afterwards via fn_poll_option_add
-- (PostgreSqlPollRepository.Add). A function (not a procedure) because the
-- repository reads its return value directly via SELECT.

DROP FUNCTION IF EXISTS public.fn_poll_create(integer, varchar);

CREATE FUNCTION public.fn_poll_create(p_thread_id INTEGER, p_question VARCHAR(200))
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_id INTEGER;
BEGIN
    INSERT INTO public.polls (thread_id, question)
    VALUES (p_thread_id, p_question)
    RETURNING id INTO v_id;
    RETURN v_id;
END;
$$;
