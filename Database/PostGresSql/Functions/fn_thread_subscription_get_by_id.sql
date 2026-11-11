-- Matches the row lookup PostgreSqlThreadSubscriptionRepository.Add() does
-- right after sp_thread_subscription_insert to return the full inserted row.

DROP FUNCTION IF EXISTS public.fn_thread_subscription_get_by_id(integer);

CREATE FUNCTION public.fn_thread_subscription_get_by_id(p_id INTEGER)
RETURNS SETOF public.thread_subscriptions
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.thread_subscriptions WHERE id = p_id;
$$;
