-- Matches IReportRepository.CountByType(ReportType type)

DROP FUNCTION IF EXISTS public.fn_report_count_by_type(smallint);

CREATE FUNCTION public.fn_report_count_by_type(p_type SMALLINT)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER FROM public.reports WHERE type = p_type;
$$;
