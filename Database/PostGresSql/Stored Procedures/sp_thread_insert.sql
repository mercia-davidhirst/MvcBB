-- Matches IForumThreadRepository.Add(ForumThread thread)

DROP PROCEDURE IF EXISTS public.sp_thread_insert(varchar, integer, integer, boolean, boolean, integer, integer, timestamptz, integer);

CREATE PROCEDURE public.sp_thread_insert(
    p_title VARCHAR(200),
    p_board_id INTEGER,
    p_created_by_user_id INTEGER,
    p_is_sticky BOOLEAN DEFAULT FALSE,
    p_is_locked BOOLEAN DEFAULT FALSE,
    p_view_count INTEGER DEFAULT 0,
    p_post_count INTEGER DEFAULT 0,
    p_last_post_at TIMESTAMPTZ DEFAULT NULL,
    p_last_post_by_user_id INTEGER DEFAULT NULL,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.threads (title, board_id, created_by_user_id, is_sticky, is_locked, view_count, post_count, last_post_at, last_post_by_user_id)
    VALUES (p_title, p_board_id, p_created_by_user_id, p_is_sticky, p_is_locked, p_view_count, p_post_count, p_last_post_at, p_last_post_by_user_id)
    RETURNING id INTO p_id;
END;
$$;
