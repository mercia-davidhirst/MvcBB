-- Matches INotificationRepository.Exists(int userId, NotificationType type, int postId, int? actorUserId)

DROP FUNCTION IF EXISTS public.fn_notification_exists(integer, smallint, integer, integer);

CREATE FUNCTION public.fn_notification_exists(p_user_id INTEGER, p_type SMALLINT, p_post_id INTEGER, p_actor_user_id INTEGER)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1 FROM public.notifications
        WHERE post_id = p_post_id
          AND user_id = p_user_id
          AND type = p_type
          AND (p_actor_user_id IS NULL OR actor_user_id = p_actor_user_id)
    );
$$;
