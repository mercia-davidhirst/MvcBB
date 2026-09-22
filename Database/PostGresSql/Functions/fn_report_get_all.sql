-- Matches IReportRepository.GetAll() (ordered by created_at desc, as in
-- InMemoryReportRepository)

DROP FUNCTION IF EXISTS public.fn_report_get_all();

CREATE FUNCTION public.fn_report_get_all()
RETURNS SETOF public.reports
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.reports ORDER BY created_at DESC;
$$;
