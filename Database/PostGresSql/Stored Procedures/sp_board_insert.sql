-- Matches IBoardRepository.Add(Board board)

DROP PROCEDURE IF EXISTS public.sp_board_insert(varchar, varchar, integer, boolean, integer, integer, integer, timestamptz);

CREATE PROCEDURE public.sp_board_insert(
    p_name VARCHAR(100),
    p_description VARCHAR(500) DEFAULT '',
    p_sort_order INTEGER DEFAULT 0,
    p_is_active BOOLEAN DEFAULT TRUE,
    p_thread_count INTEGER DEFAULT 0,
    p_post_count INTEGER DEFAULT 0,
    p_last_post_by_user_id INTEGER DEFAULT NULL,
    p_last_post_at TIMESTAMPTZ DEFAULT NULL,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.boards (name, description, sort_order, is_active, thread_count, post_count, last_post_by_user_id, last_post_at)
    VALUES (p_name, p_description, p_sort_order, p_is_active, p_thread_count, p_post_count, p_last_post_by_user_id, p_last_post_at)
    RETURNING id INTO p_id;
END;
$$;
