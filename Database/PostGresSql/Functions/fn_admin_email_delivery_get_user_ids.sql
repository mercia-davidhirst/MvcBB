-- Matches IAdminEmailRepository.GetDeliveredUserIds(int adminEmailId)

DROP FUNCTION IF EXISTS public.fn_admin_email_delivery_get_user_ids(integer);

CREATE FUNCTION public.fn_admin_email_delivery_get_user_ids(p_admin_email_id INTEGER)
RETURNS TABLE (user_id INTEGER)
LANGUAGE sql
STABLE
AS $$
    SELECT d.user_id FROM public.admin_email_deliveries d WHERE d.admin_email_id = p_admin_email_id;
$$;
