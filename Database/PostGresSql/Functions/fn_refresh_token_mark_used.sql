-- Matches IRefreshTokenRepository.MarkUsed(int id, DateTime usedAt)
-- Only marks a token that is still unused and not revoked, in one statement,
-- so two requests can never both spend the same token. Returns TRUE if this
-- call marked it.

DROP FUNCTION IF EXISTS public.fn_refresh_token_mark_used(integer, timestamptz);

CREATE FUNCTION public.fn_refresh_token_mark_used(p_id INTEGER, p_used_at TIMESTAMPTZ)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
DECLARE
    v_updated INTEGER;
BEGIN
    UPDATE public.refresh_tokens
    SET used_at = p_used_at
    WHERE id = p_id AND used_at IS NULL AND revoked_at IS NULL;
    GET DIAGNOSTICS v_updated = ROW_COUNT;
    RETURN v_updated > 0;
END;
$$;
