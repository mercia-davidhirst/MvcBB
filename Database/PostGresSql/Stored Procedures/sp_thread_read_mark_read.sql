-- Matches IThreadReadRepository.MarkRead(int threadId, string userId, int postId, DateTime postCreatedAt)
-- Creates the row the first time, then only moves it forward: a post created
-- at or before the saved position leaves the row as it is.

DROP PROCEDURE IF EXISTS public.sp_thread_read_mark_read(integer, integer, integer, timestamptz);

CREATE PROCEDURE public.sp_thread_read_mark_read(
    p_thread_id INTEGER,
    p_user_id INTEGER,
    p_last_read_post_id INTEGER,
    p_last_read_post_at TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.thread_reads (thread_id, user_id, last_read_post_id, last_read_post_at)
    VALUES (p_thread_id, p_user_id, p_last_read_post_id, p_last_read_post_at)
    ON CONFLICT (user_id, thread_id) DO UPDATE
        SET last_read_post_id = EXCLUDED.last_read_post_id,
            last_read_post_at = EXCLUDED.last_read_post_at,
            updated_at = now()
        WHERE public.thread_reads.last_read_post_at < EXCLUDED.last_read_post_at;
END;
$$;
