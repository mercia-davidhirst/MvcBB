-- Matches IPostLikeRepository.HasLiked(int postId, string userId)

DROP FUNCTION IF EXISTS public.fn_post_like_has_liked(integer, integer);

CREATE FUNCTION public.fn_post_like_has_liked(p_post_id INTEGER, p_user_id INTEGER)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (SELECT 1 FROM public.post_likes WHERE post_id = p_post_id AND user_id = p_user_id);
$$;
