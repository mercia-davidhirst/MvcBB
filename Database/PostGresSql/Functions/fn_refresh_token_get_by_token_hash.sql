-- Matches IRefreshTokenRepository.GetByTokenHash(string tokenHash)

DROP FUNCTION IF EXISTS public.fn_refresh_token_get_by_token_hash(char);

CREATE FUNCTION public.fn_refresh_token_get_by_token_hash(p_token_hash CHAR(64))
RETURNS SETOF public.refresh_tokens
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.refresh_tokens WHERE token_hash = p_token_hash;
$$;
