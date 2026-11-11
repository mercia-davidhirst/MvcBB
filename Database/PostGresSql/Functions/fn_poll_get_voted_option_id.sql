-- Matches IPollRepository.GetVotedOptionId(int pollId, string userId)

DROP FUNCTION IF EXISTS public.fn_poll_get_voted_option_id(integer, integer);

CREATE FUNCTION public.fn_poll_get_voted_option_id(p_poll_id INTEGER, p_user_id INTEGER)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT poll_option_id FROM public.poll_votes WHERE poll_id = p_poll_id AND user_id = p_user_id;
$$;
