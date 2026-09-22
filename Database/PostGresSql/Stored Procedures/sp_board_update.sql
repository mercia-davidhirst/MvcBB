-- Matches IBoardRepository.Update(Board board)

DROP PROCEDURE IF EXISTS public.sp_board_update(integer, varchar, varchar, integer, boolean, integer, integer, integer, timestamptz);

CREATE PROCEDURE public.sp_board_update(
    p_id INTEGER,
    p_name VARCHAR(100),
    p_description VARCHAR(500),
    p_sort_order INTEGER,
    p_is_active BOOLEAN,
    p_thread_count INTEGER,
    p_post_count INTEGER,
    p_last_post_by_user_id INTEGER DEFAULT NULL,
    p_last_post_at TIMESTAMPTZ DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.boards
    SET name = p_name,
        description = p_description,
        sort_order = p_sort_order,
        is_active = p_is_active,
        updated_at = now(),
        thread_count = p_thread_count,
        post_count = p_post_count,
        last_post_by_user_id = p_last_post_by_user_id,
        last_post_at = p_last_post_at
    WHERE id = p_id;
END;
$$;
