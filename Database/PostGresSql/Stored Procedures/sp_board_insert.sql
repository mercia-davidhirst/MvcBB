-- Matches IBoardRepository.Add(Board board)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_board_insert(varchar, varchar, integer, boolean, integer, boolean, boolean, integer, integer, integer, timestamptz);

CREATE PROCEDURE public.sp_board_insert(
    p_name VARCHAR(100),
    p_description VARCHAR(500),
    p_sort_order INTEGER,
    p_is_active BOOLEAN,
    p_parent_id INTEGER,
    p_is_category BOOLEAN,
    p_is_private BOOLEAN,
    p_thread_count INTEGER,
    p_post_count INTEGER,
    p_last_post_by_user_id INTEGER,
    p_last_post_at TIMESTAMPTZ,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.boards (name, description, sort_order, is_active, parent_id, is_category, is_private, thread_count, post_count, last_post_by_user_id, last_post_at)
    VALUES (p_name, p_description, p_sort_order, p_is_active, p_parent_id, p_is_category, p_is_private, p_thread_count, p_post_count, p_last_post_by_user_id, p_last_post_at)
    RETURNING id INTO p_id;
END;
$$;
