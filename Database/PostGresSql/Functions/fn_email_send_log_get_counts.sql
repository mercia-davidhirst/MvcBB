-- Matches IEmailSendLogRepository.GetCounts(DateTime fromUtc, DateTime toUtc, int bucketMinutes)
-- Counts per kind in slots of p_bucket_minutes starting at p_from. Empty
-- slots aren't returned.

DROP FUNCTION IF EXISTS public.fn_email_send_log_get_counts(timestamptz, timestamptz, integer);

CREATE FUNCTION public.fn_email_send_log_get_counts(p_from TIMESTAMPTZ, p_to TIMESTAMPTZ, p_bucket_minutes INTEGER)
RETURNS TABLE (bucket_start TIMESTAMPTZ, kind SMALLINT, count INTEGER)
LANGUAGE sql
STABLE
AS $$
    SELECT p_from + (floor(extract(epoch FROM (l.sent_at - p_from)) / 60 / p_bucket_minutes) * p_bucket_minutes) * interval '1 minute' AS bucket_start,
           l.kind,
           count(*)::integer AS count
    FROM public.email_send_log l
    WHERE l.sent_at >= p_from AND l.sent_at < p_to
    GROUP BY 1, 2
    ORDER BY 1, 2;
$$;
