-- Matches IPostLikeRepository.AddLike(int postId, string userId)
-- A no-op if the like already exists, matching the repositories' semantics.

DROP PROCEDURE IF EXISTS public.sp_post_like_add(integer, integer);

CREATE PROCEDURE public.sp_post_like_add(p_post_id INTEGER, p_user_id INTEGER)
LANGUAGE plpgsql
AS $$
DECLARE
    rows_inserted INTEGER;
BEGIN
    INSERT INTO public.post_likes (post_id, user_id)
    VALUES (p_post_id, p_user_id)
    ON CONFLICT (post_id, user_id) DO NOTHING;

    GET DIAGNOSTICS rows_inserted = ROW_COUNT;

    -- posts.like_count/threads.total_likes are maintained here rather than
    -- recomputed with COUNT(*) on every read - this proc is the only place
    -- a like is ever added, so it's the only place that needs to keep them
    -- in sync.
    IF rows_inserted > 0 THEN
        UPDATE public.posts SET like_count = like_count + 1 WHERE id = p_post_id;
        UPDATE public.threads SET total_likes = total_likes + 1
        WHERE id = (SELECT thread_id FROM public.posts WHERE id = p_post_id);
    END IF;
END;
$$;
