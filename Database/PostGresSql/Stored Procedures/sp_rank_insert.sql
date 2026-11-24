-- Matches IRankRepository.Add(RankModel rank)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_rank_insert(varchar, integer, varchar, boolean, integer);

CREATE PROCEDURE public.sp_rank_insert(
    p_title VARCHAR(70),
    p_min_posts INTEGER,
    p_image_path VARCHAR(500),
    p_is_active BOOLEAN,
    p_sort_order INTEGER,
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
