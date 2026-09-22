-- Matches IReportRepository.CountByStatus(ReportStatus status)

DROP FUNCTION IF EXISTS public.fn_report_count_by_status(smallint);

CREATE FUNCTION public.fn_report_count_by_status(p_status SMALLINT)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER FROM public.reports WHERE status = p_status;
$$;
