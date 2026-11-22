-- Matches IReportRepository.CountOpen(): reports still waiting on a
-- moderator, i.e. status 0 (Pending) or 1 (Investigating). Polled by the
-- moderator/administrator navbar alert badge.

DROP FUNCTION IF EXISTS public.fn_report_count_open();

CREATE FUNCTION public.fn_report_count_open()
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER FROM public.reports WHERE status IN (0, 1);
$$;
