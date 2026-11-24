-- Matches IEmailSendLogRepository.Prune(DateTime beforeUtc). A function
-- rather than a procedure only so it can return how many rows it deleted.

DROP FUNCTION IF EXISTS public.fn_email_send_log_prune(timestamptz);

CREATE FUNCTION public.fn_email_send_log_prune(p_before TIMESTAMPTZ)
RETURNS INTEGER
LANGUAGE plpgsql
AS $$
DECLARE
    v_count INTEGER;
BEGIN
    DELETE FROM public.email_send_log WHERE sent_at < p_before;

    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN v_count;
END;
$$;
