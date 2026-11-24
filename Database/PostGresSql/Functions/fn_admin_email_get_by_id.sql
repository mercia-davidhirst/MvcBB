-- Matches IAdminEmailRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_admin_email_get_by_id(integer);

CREATE FUNCTION public.fn_admin_email_get_by_id(p_id INTEGER)
RETURNS SETOF public.admin_emails
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.admin_emails WHERE id = p_id;
$$;
