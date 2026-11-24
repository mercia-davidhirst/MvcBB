-- Matches IMessageRepository.Add(Message message)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_message_insert(text, integer, integer, varchar);

CREATE PROCEDURE public.sp_message_insert(
    p_content TEXT,
    p_sender_user_id INTEGER,
    p_recipient_user_id INTEGER,
    p_subject VARCHAR(200),
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.messages (subject, content, sender_user_id, recipient_user_id)
    VALUES (p_subject, p_content, p_sender_user_id, p_recipient_user_id)
    RETURNING id INTO p_id;
END;
$$;
