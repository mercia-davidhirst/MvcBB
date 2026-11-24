-- Matches IAdminEmailRepository.AddDelivery(int adminEmailId, int userId, DateTime sentAt)

DROP PROCEDURE IF EXISTS public.sp_admin_email_delivery_insert(integer, integer, timestamptz);

CREATE PROCEDURE public.sp_admin_email_delivery_insert(
    p_admin_email_id INTEGER,
    p_user_id INTEGER,
    p_sent_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.admin_email_deliveries (admin_email_id, user_id, sent_at)
    VALUES (p_admin_email_id, p_user_id, COALESCE(p_sent_at, now()))
    ON CONFLICT (admin_email_id, user_id) DO NOTHING;
END;
$$;
