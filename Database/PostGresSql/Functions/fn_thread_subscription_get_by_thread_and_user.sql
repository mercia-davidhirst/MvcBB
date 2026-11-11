-- Matches IThreadSubscriptionRepository.GetByThreadAndUser(int threadId, string userId)

DROP FUNCTION IF EXISTS public.fn_thread_subscription_get_by_thread_and_user(integer, integer);

CREATE FUNCTION public.fn_thread_subscription_get_by_thread_and_user(p_thread_id INTEGER, p_user_id INTEGER)
RETURNS SETOF public.thread_subscriptions
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.thread_subscriptions WHERE thread_id = p_thread_id AND user_id = p_user_id;
$$;
