-- Matches IUserRepository.GetStats(DateTime activeSince) - the homepage
-- statistics panel's total member / recently-active member counts, as a
-- single aggregate query instead of the caller loading every user to call
-- Count()/Count(predicate) in memory.

DROP FUNCTION IF EXISTS public.fn_user_get_stats(timestamptz);

CREATE FUNCTION public.fn_user_get_stats(p_active_since TIMESTAMPTZ)
RETURNS TABLE(total_members BIGINT, active_members BIGINT)
LANGUAGE sql
STABLE
AS $$
    SELECT
        COUNT(*) AS total_members,
        COUNT(*) FILTER (WHERE last_login_at >= p_active_since) AS active_members
    FROM public.users;
$$;
