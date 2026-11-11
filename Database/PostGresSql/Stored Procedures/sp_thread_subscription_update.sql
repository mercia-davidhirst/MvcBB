-- Matches IThreadSubscriptionRepository.Update(ThreadSubscriptionModel subscription)

DROP PROCEDURE IF EXISTS public.sp_thread_subscription_update(integer, boolean, boolean);

CREATE PROCEDURE public.sp_thread_subscription_update(
    p_id INTEGER,
    p_notify_via_message BOOLEAN,
    p_notify_via_email BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.thread_subscriptions
    SET notify_via_message = p_notify_via_message,
        notify_via_email = p_notify_via_email
    WHERE id = p_id;
END;
$$;
