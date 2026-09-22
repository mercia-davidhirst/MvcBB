-- Matches IReportRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_report_get_by_id(integer);

CREATE FUNCTION public.fn_report_get_by_id(p_id INTEGER)
RETURNS SETOF public.reports
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.reports WHERE id = p_id;
$$;
