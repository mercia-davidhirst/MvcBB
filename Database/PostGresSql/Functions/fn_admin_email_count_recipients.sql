-- Matches IAdminEmailRepository.CountRecipients(UserRole? role, int? userId) -
-- the same filter as fn_admin_email_get_recipients, as counts.

DROP FUNCTION IF EXISTS public.fn_admin_email_count_recipients(smallint, integer);

CREATE FUNCTION public.fn_admin_email_count_recipients(
    p_role SMALLINT,
    p_user_id INTEGER
)
RETURNS TABLE (total INTEGER, opted_out INTEGER)
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::integer, COUNT(o.user_id)::integer
    FROM public.users u
    LEFT JOIN public.admin_email_opt_outs o ON o.user_id = u.id
    WHERE u.role <> 3
      AND u.email <> ''
      AND (p_role IS NULL OR u.role = p_role)
      AND (p_user_id IS NULL OR u.id = p_user_id);
$$;
