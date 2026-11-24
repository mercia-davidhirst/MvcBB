-- Matches IAdminEmailRepository.Add(AdminEmail email)

DROP PROCEDURE IF EXISTS public.sp_admin_email_insert(integer, varchar, varchar, text, smallint, smallint, integer, smallint, varchar, varchar, varchar, boolean, smallint, integer);

CREATE PROCEDURE public.sp_admin_email_insert(
    p_sent_by_user_id INTEGER,
    p_sent_by_username VARCHAR(50),
    p_subject VARCHAR(200),
    p_body TEXT,
    p_audience SMALLINT,
    p_role SMALLINT,
    p_user_id INTEGER,
    p_sender_mode SMALLINT,
    p_from_address VARCHAR(256),
    p_from_name VARCHAR(256),
    p_reply_to_address VARCHAR(256),
    p_is_important BOOLEAN,
    p_status SMALLINT,
    p_recipient_count INTEGER,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.admin_emails (sent_by_user_id, sent_by_username, subject, body, audience, role, user_id,
                                     sender_mode, from_address, from_name, reply_to_address, is_important, status, recipient_count)
    VALUES (p_sent_by_user_id, p_sent_by_username, p_subject, p_body, p_audience, p_role, p_user_id,
            p_sender_mode, p_from_address, p_from_name, p_reply_to_address, p_is_important, p_status, p_recipient_count)
    RETURNING id INTO p_id;
END;
$$;
