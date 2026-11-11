-- Matches IPollRepository.AddVote(int pollId, int pollOptionId, string userId)

DROP PROCEDURE IF EXISTS public.sp_poll_vote_add(integer, integer, integer);

CREATE PROCEDURE public.sp_poll_vote_add(p_poll_id INTEGER, p_poll_option_id INTEGER, p_user_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.poll_votes (poll_id, poll_option_id, user_id)
    VALUES (p_poll_id, p_poll_option_id, p_user_id);
END;
$$;
