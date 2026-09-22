-- Matches IMessageRepository.GetByRecipientUserId(string userId, bool unreadOnly = false)

DROP FUNCTION IF EXISTS public.fn_message_get_by_recipient_user_id(integer, boolean);

CREATE FUNCTION public.fn_message_get_by_recipient_user_id(p_recipient_user_id INTEGER, p_unread_only BOOLEAN DEFAULT FALSE)
RETURNS SETOF public.messages
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.messages
    WHERE recipient_user_id = p_recipient_user_id
      AND (p_unread_only = FALSE OR read_at IS NULL)
    ORDER BY created_at DESC;
$$;
