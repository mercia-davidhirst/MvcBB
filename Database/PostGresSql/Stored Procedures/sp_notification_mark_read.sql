-- Matches INotificationRepository.MarkRead, MarkAllRead, MarkReadForThread
-- and MarkReadForMessage: marks the member's unread notifications as read -
-- just p_id, just those about p_thread_id or p_message_id, or (all three
-- null) every one of them. Only ever touches p_user_id's own rows.

DROP PROCEDURE IF EXISTS public.sp_notification_mark_read(integer, integer, integer, integer);

CREATE PROCEDURE public.sp_notification_mark_read(
    p_user_id INTEGER,
    p_id INTEGER,
    p_thread_id INTEGER,
    p_message_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.notifications
    SET is_read = TRUE
    WHERE user_id = p_user_id
      AND is_read = FALSE
      AND (p_id IS NULL OR id = p_id)
      AND (p_thread_id IS NULL OR thread_id = p_thread_id)
      AND (p_message_id IS NULL OR message_id = p_message_id);
END;
$$;
