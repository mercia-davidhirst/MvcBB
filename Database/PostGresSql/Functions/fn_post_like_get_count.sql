-- Matches IPostLikeRepository.GetLikeCount(int postId)

DROP FUNCTION IF EXISTS public.fn_post_like_get_count(integer);

CREATE FUNCTION public.fn_post_like_get_count(p_post_id INTEGER)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER FROM public.post_likes WHERE post_id = p_post_id;
$$;
