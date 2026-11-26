-- Matches IModerationQueueRepository.Add(ModerationQueueItem item)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_moderation_queue_insert(smallint, integer, integer, integer, varchar, text, integer, varchar, text);

CREATE PROCEDURE public.sp_moderation_queue_insert(
    p_kind SMALLINT,
    p_user_id INTEGER,
    p_board_id INTEGER,
    p_thread_id INTEGER,
    p_title VARCHAR(200),
    p_content TEXT,
    p_quoted_post_id INTEGER,
    p_image_url VARCHAR(500),
    p_poll_json TEXT,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.moderation_queue (kind, user_id, board_id, thread_id, title, content, quoted_post_id, image_url, poll_json)
    VALUES (p_kind, p_user_id, p_board_id, p_thread_id, p_title, p_content, p_quoted_post_id, p_image_url, p_poll_json)
    RETURNING id INTO p_id;
END;
$$;
