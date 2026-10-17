-- Matches IPasswordResetTokenRepository.GetByTokenHash(string tokenHash)

DROP FUNCTION IF EXISTS public.fn_password_reset_token_get_by_token_hash(char);

CREATE FUNCTION public.fn_password_reset_token_get_by_token_hash(p_token_hash CHAR(64))
RETURNS SETOF public.password_reset_tokens
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.password_reset_tokens WHERE token_hash = p_token_hash;
$$;
