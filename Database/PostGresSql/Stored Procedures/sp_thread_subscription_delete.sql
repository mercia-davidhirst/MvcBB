-- Matches IThreadSubscriptionRepository.Remove(int threadId, string userId)

DROP PROCEDURE IF EXISTS public.sp_thread_subscription_delete(integer, integer);

CREATE PROCEDURE public.sp_thread_subscription_delete(p_thread_id INTEGER, p_user_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.thread_subscriptions WHERE thread_id = p_thread_id AND user_id = p_user_id;
END;
$$;
