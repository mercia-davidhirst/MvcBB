-- Matches IAdminEmailRepository.SetOptedOut(int userId, bool optedOut)

DROP PROCEDURE IF EXISTS public.sp_admin_email_opt_out_set(integer, boolean);

CREATE PROCEDURE public.sp_admin_email_opt_out_set(
    p_user_id INTEGER,
    p_opted_out BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_opted_out THEN
        INSERT INTO public.admin_email_opt_outs (user_id)
        VALUES (p_user_id)
        ON CONFLICT (user_id) DO NOTHING;
    ELSE
        DELETE FROM public.admin_email_opt_outs WHERE user_id = p_user_id;
    END IF;
END;
$$;
