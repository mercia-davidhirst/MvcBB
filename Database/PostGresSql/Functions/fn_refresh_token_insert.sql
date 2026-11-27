-- Matches IRefreshTokenRepository.Add(RefreshToken token)
-- A function (not a procedure) so the new row comes straight back.

DROP FUNCTION IF EXISTS public.fn_refresh_token_insert(integer, char, uuid, varchar, timestamptz, timestamptz, varchar);

CREATE FUNCTION public.fn_refresh_token_insert(
    p_user_id INTEGER,
    p_token_hash CHAR(64),
    p_family_id UUID,
    p_session_stamp VARCHAR(64),
    p_created_at TIMESTAMPTZ,
    p_expires_at TIMESTAMPTZ,
    p_created_ip VARCHAR(45)
)
RETURNS SETOF public.refresh_tokens
LANGUAGE sql
AS $$
    INSERT INTO public.refresh_tokens (user_id, token_hash, family_id, session_stamp, created_at, expires_at, created_ip)
    VALUES (p_user_id, p_token_hash, p_family_id, p_session_stamp, COALESCE(p_created_at, now()), p_expires_at, p_created_ip)
    RETURNING *;
$$;
