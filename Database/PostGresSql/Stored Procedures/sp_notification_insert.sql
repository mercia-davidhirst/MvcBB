-- Matches INotificationRepository.Add(Notification notification)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_notification_insert(integer, smallint, integer, integer, integer, integer, varchar);

CREATE PROCEDURE public.sp_notification_insert(
    p_user_id INTEGER,
    p_type SMALLINT,
    p_actor_user_id INTEGER,
    p_thread_id INTEGER,
    p_post_id INTEGER,
    p_message_id INTEGER,
    p_excerpt VARCHAR(300),
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.notifications (user_id, type, actor_user_id, thread_id, post_id, message_id, excerpt)
    VALUES (p_user_id, p_type, p_actor_user_id, p_thread_id, p_post_id, p_message_id, p_excerpt)
    RETURNING id INTO p_id;
END;
$$;
