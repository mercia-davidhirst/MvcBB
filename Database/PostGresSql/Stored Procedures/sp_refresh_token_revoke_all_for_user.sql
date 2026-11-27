-- Matches IRefreshTokenRepository.RevokeAllForUser(int userId)
-- Ends every "Remember me" sign-in a member has, on every device.

DROP PROCEDURE IF EXISTS public.sp_refresh_token_revoke_all_for_user(integer);

CREATE PROCEDURE public.sp_refresh_token_revoke_all_for_user(p_user_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.refresh_tokens
    SET revoked_at = now()
    WHERE user_id = p_user_id AND revoked_at IS NULL;
END;
$$;
