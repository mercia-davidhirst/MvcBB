-- Matches IPollRepository.GetVoteCounts(int pollId) - options with zero votes
-- simply aren't in the result; the caller (ThreadsController.MapToPollResponse)
-- already treats a missing key as 0. vote_count is cast to INTEGER (COUNT(*)
-- is BIGINT by default) to match PostgreSqlPollRepository.VoteCountRow.VoteCount.

DROP FUNCTION IF EXISTS public.fn_poll_get_vote_counts(integer);

CREATE FUNCTION public.fn_poll_get_vote_counts(p_poll_id INTEGER)
RETURNS TABLE(poll_option_id INTEGER, vote_count INTEGER)
LANGUAGE sql
STABLE
AS $$
    SELECT poll_option_id, CAST(COUNT(*) AS INTEGER) AS vote_count
    FROM public.poll_votes
    WHERE poll_id = p_poll_id
    GROUP BY poll_option_id;
$$;
