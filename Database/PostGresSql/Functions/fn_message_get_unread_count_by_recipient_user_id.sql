-- Matches IMessageRepository.GetUnreadCountByRecipientUserId(string userId)

DROP FUNCTION IF EXISTS public.fn_message_get_unread_count_by_recipient_user_id(integer);

CREATE FUNCTION public.fn_message_get_unread_count_by_recipient_user_id(p_recipient_user_id INTEGER)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER
    FROM public.messages
    WHERE recipient_user_id = p_recipient_user_id
      AND read_at IS NULL;
$$;
