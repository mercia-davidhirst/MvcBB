-- Matches IEmailSendLogRepository.GetSentTimesSince(DateTime sinceUtc)

DROP FUNCTION IF EXISTS public.fn_email_send_log_get_sent_since(timestamptz);

CREATE FUNCTION public.fn_email_send_log_get_sent_since(p_since TIMESTAMPTZ)
RETURNS TABLE (sent_at TIMESTAMPTZ)
LANGUAGE sql
STABLE
AS $$
    SELECT l.sent_at FROM public.email_send_log l WHERE l.sent_at > p_since ORDER BY l.sent_at;
$$;
