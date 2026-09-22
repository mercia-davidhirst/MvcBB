-- Matches IBoardRepository.GetAll() (ordered by sort_order)

DROP FUNCTION IF EXISTS public.fn_board_get_all();

CREATE FUNCTION public.fn_board_get_all()
RETURNS SETOF public.boards
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.boards ORDER BY sort_order;
$$;
