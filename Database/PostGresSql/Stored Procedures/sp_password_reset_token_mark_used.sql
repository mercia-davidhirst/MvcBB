-- Matches IPasswordResetTokenRepository.MarkUsed(int id)

DROP PROCEDURE IF EXISTS public.sp_password_reset_token_mark_used(integer);

CREATE PROCEDURE public.sp_password_reset_token_mark_used(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.password_reset_tokens
    SET used_at = now()
    WHERE id = p_id;
END;
$$;
