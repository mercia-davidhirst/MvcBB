-- Matches IRankRepository.Add(RankModel rank)

DROP PROCEDURE IF EXISTS public.sp_rank_insert(varchar, integer, varchar, boolean, integer);

CREATE PROCEDURE public.sp_rank_insert(
    p_title VARCHAR(70),
    p_min_posts INTEGER DEFAULT 0,
    p_image_path VARCHAR(500) DEFAULT NULL,
    p_is_active BOOLEAN DEFAULT TRUE,
    p_sort_order INTEGER DEFAULT 0,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.ranks (title, min_posts, image_path, is_active, sort_order)
    VALUES (p_title, p_min_posts, p_image_path, p_is_active, p_sort_order)
    RETURNING id INTO p_id;
END;
$$;
