-- Matches IPostLikeRepository.RemoveLike(int postId, string userId)

DROP PROCEDURE IF EXISTS public.sp_post_like_remove(integer, integer);

CREATE PROCEDURE public.sp_post_like_remove(p_post_id INTEGER, p_user_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.post_likes WHERE post_id = p_post_id AND user_id = p_user_id;
END;
$$;
