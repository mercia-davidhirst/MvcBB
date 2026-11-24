-- Matches IThreadSubscriptionRepository.Add(ThreadSubscriptionModel subscription)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_thread_subscription_insert(integer, integer, boolean, boolean);

CREATE PROCEDURE public.sp_thread_subscription_insert(
    p_thread_id INTEGER,
    p_user_id INTEGER,
    p_notify_via_message BOOLEAN,
    p_notify_via_email BOOLEAN,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.thread_subscriptions (thread_id, user_id, notify_via_message, notify_via_email)
    VALUES (p_thread_id, p_user_id, p_notify_via_message, p_notify_via_email)
    RETURNING id INTO p_id;
END;
$$;
