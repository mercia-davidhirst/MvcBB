-- Matches IAdminEmailRepository.UpdateProgress(AdminEmail email)

DROP PROCEDURE IF EXISTS public.sp_admin_email_update_progress(integer, smallint, integer, integer, integer, timestamptz);
DROP PROCEDURE IF EXISTS public.sp_admin_email_update_progress(integer, smallint, integer, integer, integer, timestamptz, timestamptz);

CREATE PROCEDURE public.sp_admin_email_update_progress(
    p_id INTEGER,
    p_status SMALLINT,
    p_recipient_count INTEGER,
    p_sent_count INTEGER,
    p_failed_count INTEGER,
    p_completed_at TIMESTAMPTZ,
    p_paused_until TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.admin_emails
    SET status = p_status,
        recipient_count = p_recipient_count,
        sent_count = p_sent_count,
        failed_count = p_failed_count,
        completed_at = p_completed_at,
        paused_until = p_paused_until
    WHERE id = p_id;
END;
$$;
