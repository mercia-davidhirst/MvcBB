-- Matches IMessageRepository.GetByRecipientOrSenderUserId(string userId)

DROP FUNCTION IF EXISTS public.fn_message_get_by_recipient_or_sender_user_id(integer);

CREATE FUNCTION public.fn_message_get_by_recipient_or_sender_user_id(p_user_id INTEGER)
RETURNS SETOF public.messages
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.messages
    WHERE recipient_user_id = p_user_id OR sender_user_id = p_user_id
    ORDER BY created_at DESC;
$$;
