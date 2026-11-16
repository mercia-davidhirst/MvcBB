-- Matches IPostLikeRepository.RemoveLike(int postId, string userId)

DROP PROCEDURE IF EXISTS public.sp_post_like_remove(integer, integer);

CREATE PROCEDURE public.sp_post_like_remove(p_post_id INTEGER, p_user_id INTEGER)
LANGUAGE plpgsql
AS $$
DECLARE
    rows_deleted INTEGER;
BEGIN
    DELETE FROM public.post_likes WHERE post_id = p_post_id AND user_id = p_user_id;
    GET DIAGNOSTICS rows_deleted = ROW_COUNT;

    IF rows_deleted > 0 THEN
        UPDATE public.posts SET like_count = like_count - 1 WHERE id = p_post_id;
        UPDATE public.threads SET total_likes = total_likes - 1
        WHERE id = (SELECT thread_id FROM public.posts WHERE id = p_post_id);
    END IF;
END;
$$;
