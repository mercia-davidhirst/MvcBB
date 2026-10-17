-- Matches IPasswordResetTokenRepository.Add(PasswordResetToken token)

DROP PROCEDURE IF EXISTS public.sp_password_reset_token_insert(integer, char, timestamptz);

CREATE PROCEDURE public.sp_password_reset_token_insert(
    p_user_id INTEGER,
    p_token_hash CHAR(64),
    p_expires_at TIMESTAMPTZ,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.password_reset_tokens (user_id, token_hash, expires_at)
    VALUES (p_user_id, p_token_hash, p_expires_at)
    RETURNING id INTO p_id;
END;
$$;
