-- Matches IPollRepository.HasVoted(int pollId, string userId)

DROP FUNCTION IF EXISTS public.fn_poll_has_voted(integer, integer);

CREATE FUNCTION public.fn_poll_has_voted(p_poll_id INTEGER, p_user_id INTEGER)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1 FROM public.poll_votes WHERE poll_id = p_poll_id AND user_id = p_user_id
    );
$$;
