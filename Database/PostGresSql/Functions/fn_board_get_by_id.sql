-- Matches IBoardRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_board_get_by_id(integer);

CREATE FUNCTION public.fn_board_get_by_id(p_id INTEGER)
RETURNS SETOF public.boards
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.boards WHERE id = p_id;
$$;
