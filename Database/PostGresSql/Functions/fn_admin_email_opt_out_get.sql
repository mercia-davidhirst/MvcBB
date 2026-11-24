-- Matches IAdminEmailRepository.IsOptedOut(int userId)

DROP FUNCTION IF EXISTS public.fn_admin_email_opt_out_get(integer);

CREATE FUNCTION public.fn_admin_email_opt_out_get(p_user_id INTEGER)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (SELECT 1 FROM public.admin_email_opt_outs WHERE user_id = p_user_id);
$$;
