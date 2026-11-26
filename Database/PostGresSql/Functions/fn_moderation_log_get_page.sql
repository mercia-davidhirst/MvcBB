-- Matches IModerationLogRepository.GetPage(int skip, int take)
-- Newest first, with both members' usernames. total_count is the full log
-- size, on every row.

DROP FUNCTION IF EXISTS public.fn_moderation_log_get_page(integer, integer);

CREATE FUNCTION public.fn_moderation_log_get_page(p_skip INTEGER, p_take INTEGER)
RETURNS TABLE (
    id INTEGER,
    actor_user_id INTEGER,
    actor_username TEXT,
    action VARCHAR(50),
    target_type VARCHAR(20),
    target_id INTEGER,
    target_user_id INTEGER,
    target_username TEXT,
    summary VARCHAR(500),
    reason VARCHAR(500),
    created_at TIMESTAMPTZ,
    total_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT l.id, l.actor_user_id, a.username::text, l.action, l.target_type,
           l.target_id, l.target_user_id, tu.username::text, l.summary,
           l.reason, l.created_at,
           COUNT(*) OVER ()
    FROM public.moderation_log AS l
    LEFT JOIN public.users AS a ON a.id = l.actor_user_id
    LEFT JOIN public.users AS tu ON tu.id = l.target_user_id
    ORDER BY l.created_at DESC, l.id DESC
    OFFSET p_skip LIMIT p_take;
$$;
