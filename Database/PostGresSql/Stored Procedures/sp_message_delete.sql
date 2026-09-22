-- Matches IMessageRepository.Remove(Message message)

DROP PROCEDURE IF EXISTS public.sp_message_delete(integer);

CREATE PROCEDURE public.sp_message_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.messages WHERE id = p_id;
END;
$$;
