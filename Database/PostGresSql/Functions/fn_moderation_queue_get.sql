-- Matches IModerationQueueRepository.GetAll() (p_id null: everything, oldest
-- first) and GetById(int id). Adds the author's username, the board's name
-- and, for a reply, the thread's title.

DROP FUNCTION IF EXISTS public.fn_moderation_queue_get(integer);

CREATE FUNCTION public.fn_moderation_queue_get(p_id INTEGER)
RETURNS TABLE (
    id INTEGER,
    kind SMALLINT,
    user_id INTEGER,
    username TEXT,
    board_id INTEGER,
    board_name VARCHAR(100),
    thread_id INTEGER,
    thread_title VARCHAR(200),
    title VARCHAR(200),
    content TEXT,
    quoted_post_id INTEGER,
    image_url VARCHAR(500),
    poll_json TEXT,
    created_at TIMESTAMPTZ
)
LANGUAGE sql
STABLE
AS $$
    SELECT q.id, q.kind, q.user_id, COALESCE(u.username::text, ''),
           q.board_id, b.name, q.thread_id, t.title,
           q.title, q.content, q.quoted_post_id, q.image_url, q.poll_json, q.created_at
    FROM public.moderation_queue AS q
    LEFT JOIN public.users AS u ON u.id = q.user_id
    LEFT JOIN public.boards AS b ON b.id = q.board_id
    LEFT JOIN public.threads AS t ON t.id = q.thread_id
    WHERE p_id IS NULL OR q.id = p_id
    ORDER BY q.created_at, q.id;
$$;
