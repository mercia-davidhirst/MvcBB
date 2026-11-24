-- Matches IAdminEmailRepository.GetRecent(int take)

DROP FUNCTION IF EXISTS public.fn_admin_email_get_recent(integer);

CREATE FUNCTION public.fn_admin_email_get_recent(p_take INTEGER)
RETURNS SETOF public.admin_emails
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.admin_emails ORDER BY created_at DESC, id DESC LIMIT p_take;
$$;
