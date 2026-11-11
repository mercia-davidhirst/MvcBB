-- Matches IThreadSubscriptionRepository.GetByThreadId(int threadId)

DROP FUNCTION IF EXISTS public.fn_thread_subscription_get_by_thread_id(integer);

CREATE FUNCTION public.fn_thread_subscription_get_by_thread_id(p_thread_id INTEGER)
RETURNS SETOF public.thread_subscriptions
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.thread_subscriptions WHERE thread_id = p_thread_id;
$$;
