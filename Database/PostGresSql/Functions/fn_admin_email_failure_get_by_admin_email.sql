-- Matches IAdminEmailRepository.GetFailures(int adminEmailId)

DROP FUNCTION IF EXISTS public.fn_admin_email_failure_get_by_admin_email(integer);

CREATE FUNCTION public.fn_admin_email_failure_get_by_admin_email(p_admin_email_id INTEGER)
RETURNS SETOF public.admin_email_failures
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.admin_email_failures WHERE admin_email_id = p_admin_email_id ORDER BY failed_at, id;
$$;
