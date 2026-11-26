-- Matches IModerationQueueRepository.Count() (p_user_id null) and
-- CountByUser(int userId).

DROP FUNCTION IF EXISTS public.fn_moderation_queue_count(integer);

CREATE FUNCTION public.fn_moderation_queue_count(p_user_id INTEGER)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::integer FROM public.moderation_queue WHERE p_user_id IS NULL OR user_id = p_user_id;
$$;
