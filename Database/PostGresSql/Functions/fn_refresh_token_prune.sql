-- Matches IRefreshTokenRepository.Prune(DateTime now, DateTime spentBefore)
-- Deletes expired tokens, and used or revoked tokens older than the cutoff;
-- returns how many went.

DROP FUNCTION IF EXISTS public.fn_refresh_token_prune(timestamptz, timestamptz);

CREATE FUNCTION public.fn_refresh_token_prune(p_now TIMESTAMPTZ, p_spent_before TIMESTAMPTZ)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted INTEGER;
BEGIN
    DELETE FROM public.refresh_tokens
    WHERE expires_at < p_now
       OR used_at < p_spent_before
       OR revoked_at < p_spent_before;
    GET DIAGNOSTICS v_deleted = ROW_COUNT;
    RETURN v_deleted;
END;
$$;
