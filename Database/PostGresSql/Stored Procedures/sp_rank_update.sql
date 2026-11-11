-- Matches IRankRepository.Update(RankModel rank)

DROP PROCEDURE IF EXISTS public.sp_rank_update(integer, varchar, integer, varchar, boolean, integer);

CREATE PROCEDURE public.sp_rank_update(
    p_id INTEGER,
    p_title VARCHAR(70),
    p_min_posts INTEGER,
    p_image_path VARCHAR(500),
    p_is_active BOOLEAN,
    p_sort_order INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.ranks
    SET title = p_title,
        min_posts = p_min_posts,
        image_path = p_image_path,
        is_active = p_is_active,
        sort_order = p_sort_order
    WHERE id = p_id;
END;
$$;
