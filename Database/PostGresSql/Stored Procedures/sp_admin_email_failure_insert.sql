-- Matches IAdminEmailRepository.AddFailure(AdminEmailFailure failure)

DROP PROCEDURE IF EXISTS public.sp_admin_email_failure_insert(integer, integer, varchar, varchar, varchar, timestamptz);

CREATE PROCEDURE public.sp_admin_email_failure_insert(
    p_admin_email_id INTEGER,
    p_user_id INTEGER,
    p_username VARCHAR(50),
    p_email VARCHAR(256),
    p_reason VARCHAR(1000),
    p_failed_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.admin_email_failures (admin_email_id, user_id, username, email, reason, failed_at)
    VALUES (p_admin_email_id, p_user_id, p_username, p_email, p_reason, COALESCE(p_failed_at, now()));
END;
$$;
