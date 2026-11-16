-- Matches IForumThreadRepository.Update(ForumThread thread)

DROP PROCEDURE IF EXISTS public.sp_thread_update(integer, varchar, integer, boolean, boolean, integer, integer, timestamptz, integer);
DROP PROCEDURE IF EXISTS public.sp_thread_update(integer, varchar, integer, boolean, boolean, integer, integer, integer, timestamptz, integer);

-- total_likes is deliberately NOT part of this proc - it's owned
-- exclusively by sp_post_like_add/remove. This Update is called from
-- several places (title/lock edits, view/post count bumps, participant
-- count bumps) each working from a ForumThread snapshot that may be older
-- than the latest like/unlike - including total_likes here would risk
-- overwriting it with that stale snapshot.
CREATE PROCEDURE public.sp_thread_update(
    p_id INTEGER,
    p_title VARCHAR(200),
    p_board_id INTEGER,
    p_is_sticky BOOLEAN,
    p_is_locked BOOLEAN,
    p_view_count INTEGER,
    p_post_count INTEGER,
    p_participant_count INTEGER,
    p_last_post_at TIMESTAMPTZ DEFAULT NULL,
    p_last_post_by_user_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.threads
    SET title = p_title,
        board_id = p_board_id,
        updated_at = now(),
        is_sticky = p_is_sticky,
        is_locked = p_is_locked,
        view_count = p_view_count,
        post_count = p_post_count,
        participant_count = p_participant_count,
        last_post_at = p_last_post_at,
        last_post_by_user_id = p_last_post_by_user_id
    WHERE id = p_id;
END;
$$;
