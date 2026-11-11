-- Matches IPollRepository.GetByThreadId/GetById's option lookup
-- (PostgreSqlPollRepository.WithOptions)

DROP FUNCTION IF EXISTS public.fn_poll_option_get_by_poll_id(integer);

CREATE FUNCTION public.fn_poll_option_get_by_poll_id(p_poll_id INTEGER)
RETURNS SETOF public.poll_options
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.poll_options WHERE poll_id = p_poll_id ORDER BY sort_order;
$$;
