-- Matches IMessageRepository.Update(Message message)
-- In practice only read_at changes post-creation (see MessagesController
-- marking a message read), but the repository's Update takes the whole
-- object, so this allows updating all mutable fields.

DROP PROCEDURE IF EXISTS public.sp_message_update(integer, varchar, text, timestamptz);

CREATE PROCEDURE public.sp_message_update(
    p_id INTEGER,
    p_subject VARCHAR(200),
    p_content TEXT,
    p_read_at TIMESTAMPTZ DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.messages
    SET subject = p_subject,
        content = p_content,
        read_at = p_read_at
    WHERE id = p_id;
END;
$$;
