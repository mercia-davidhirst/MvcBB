-- Matches IEmailSendLogRepository.Add(DateTime sentAtUtc, EmailKind kind)

DROP PROCEDURE IF EXISTS public.sp_email_send_log_insert(timestamptz, smallint);

CREATE PROCEDURE public.sp_email_send_log_insert(
    p_sent_at TIMESTAMPTZ,
    p_kind SMALLINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.email_send_log (sent_at, kind)
    VALUES (COALESCE(p_sent_at, now()), p_kind);
END;
$$;
