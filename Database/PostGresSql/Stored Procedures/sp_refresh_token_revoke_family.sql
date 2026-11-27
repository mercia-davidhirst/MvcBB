-- Matches IRefreshTokenRepository.RevokeFamily(Guid familyId)
-- Ends one "Remember me" sign-in (every token rotated from it).

DROP PROCEDURE IF EXISTS public.sp_refresh_token_revoke_family(uuid);

CREATE PROCEDURE public.sp_refresh_token_revoke_family(p_family_id UUID)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.refresh_tokens
    SET revoked_at = now()
    WHERE family_id = p_family_id AND revoked_at IS NULL;
END;
$$;
