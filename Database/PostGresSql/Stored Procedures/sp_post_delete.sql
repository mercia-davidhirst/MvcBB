-- Matches IPostRepository.Remove(Post post)

DROP PROCEDURE IF EXISTS public.sp_post_delete(integer);

CREATE PROCEDURE public.sp_post_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.posts WHERE id = p_id;
END;
$$;
