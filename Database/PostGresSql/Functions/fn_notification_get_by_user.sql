-- Matches INotificationRepository.GetByUser(int userId, int skip, int take)
-- and GetById(int id) (pass p_id; p_user_id is then ignored).
-- Newest first, with the actor's username and the thread's current title.
-- total_count is the member's full notification count, on every row.

DROP FUNCTION IF EXISTS public.fn_notification_get_by_user(integer, integer, integer, integer);

CREATE FUNCTION public.fn_notification_get_by_user(p_user_id INTEGER, p_skip INTEGER, p_take INTEGER, p_id INTEGER)
RETURNS TABLE (
    id INTEGER,
    user_id INTEGER,
    type SMALLINT,
    actor_user_id INTEGER,
    actor_username TEXT,
    thread_id INTEGER,
    thread_title VARCHAR(200),
    post_id INTEGER,
    message_id INTEGER,
    excerpt VARCHAR(300),
    is_read BOOLEAN,
    created_at TIMESTAMPTZ,
    total_count BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT n.id, n.user_id, n.type, n.actor_user_id, a.username::text,
           n.thread_id, t.title, n.post_id, n.message_id, n.excerpt,
           n.is_read, n.created_at,
           COUNT(*) OVER ()
    FROM public.notifications AS n
    LEFT JOIN public.users AS a ON a.id = n.actor_user_id
    LEFT JOIN public.threads AS t ON t.id = n.thread_id
    WHERE (p_id IS NOT NULL AND n.id = p_id)
       OR (p_id IS NULL AND n.user_id = p_user_id)
    ORDER BY n.created_at DESC, n.id DESC
    OFFSET p_skip LIMIT p_take;
$$;
