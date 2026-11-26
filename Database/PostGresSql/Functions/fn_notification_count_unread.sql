-- Matches INotificationRepository.CountUnread(int userId)

DROP FUNCTION IF EXISTS public.fn_notification_count_unread(integer);

CREATE FUNCTION public.fn_notification_count_unread(p_user_id INTEGER)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::integer FROM public.notifications WHERE user_id = p_user_id AND is_read = FALSE;
$$;
