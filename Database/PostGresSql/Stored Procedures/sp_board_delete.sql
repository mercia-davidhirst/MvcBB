-- Matches IBoardRepository.Remove(Board board)

DROP PROCEDURE IF EXISTS public.sp_board_delete(integer);

CREATE PROCEDURE public.sp_board_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.boards WHERE id = p_id;
END;
$$;
