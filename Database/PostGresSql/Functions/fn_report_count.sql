-- Matches IReportRepository.Count()

DROP FUNCTION IF EXISTS public.fn_report_count();

CREATE FUNCTION public.fn_report_count()
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER FROM public.reports;
$$;
