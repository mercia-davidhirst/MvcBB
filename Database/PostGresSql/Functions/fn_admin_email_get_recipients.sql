-- Matches IAdminEmailRepository.GetRecipients(UserRole? role, int? userId,
-- bool includeOptedOut). Deleted (role 3) accounts and blank emails are
-- never included.

DROP FUNCTION IF EXISTS public.fn_admin_email_get_recipients(smallint, integer, boolean);

CREATE FUNCTION public.fn_admin_email_get_recipients(
    p_role SMALLINT,
    p_user_id INTEGER,
    p_include_opted_out BOOLEAN
)
RETURNS TABLE (user_id INTEGER, username TEXT, email TEXT)
LANGUAGE sql
STABLE
AS $$
    SELECT u.id, u.username::text, u.email::text
    FROM public.users u
    WHERE u.role <> 3
      AND u.email <> ''
      AND (p_role IS NULL OR u.role = p_role)
      AND (p_user_id IS NULL OR u.id = p_user_id)
      AND (p_include_opted_out OR NOT EXISTS (SELECT 1 FROM public.admin_email_opt_outs o WHERE o.user_id = u.id))
    ORDER BY u.id;
$$;
