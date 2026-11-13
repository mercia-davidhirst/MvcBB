-- Matches IPostLikeRepository.AddLike(int postId, string userId)
-- A no-op if the like already exists, matching the repositories' semantics.

DROP PROCEDURE IF EXISTS public.sp_post_like_add(integer, integer);

CREATE PROCEDURE public.sp_post_like_add(p_post_id INTEGER, p_user_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.post_likes (post_id, user_id)
    VALUES (p_post_id, p_user_id)
    ON CONFLICT (post_id, user_id) DO NOTHING;
END;
$$;
