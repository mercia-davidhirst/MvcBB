-- Matches IForumThreadRepository.Add(ForumThread thread)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_thread_insert(varchar, integer, integer, boolean, boolean, integer, integer, timestamptz, integer);
DROP PROCEDURE IF EXISTS public.sp_thread_insert(varchar, integer, integer, boolean, boolean, integer, integer, integer, timestamptz, integer);

-- total_likes is deliberately not a parameter here - it's owned exclusively
-- by sp_post_like_add/remove (always 0 for a brand new thread anyway, via
-- the column's own default).
CREATE PROCEDURE public.sp_thread_insert(
    p_title VARCHAR(200),
    p_board_id INTEGER,
    p_created_by_user_id INTEGER,
    p_is_sticky BOOLEAN,
    p_is_locked BOOLEAN,
    p_view_count INTEGER,
    p_post_count INTEGER,
    p_participant_count INTEGER,
    p_last_post_at TIMESTAMPTZ,
    p_last_post_by_user_id INTEGER,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.threads (title, board_id, created_by_user_id, is_sticky, is_locked, view_count, post_count, participant_count, last_post_at, last_post_by_user_id)
    VALUES (p_title, p_board_id, p_created_by_user_id, p_is_sticky, p_is_locked, p_view_count, p_post_count, p_participant_count, p_last_post_at, p_last_post_by_user_id)
    RETURNING id INTO p_id;
END;
$$;
