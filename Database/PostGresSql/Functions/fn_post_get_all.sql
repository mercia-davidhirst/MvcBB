-- Matches IPostRepository.GetAll()
-- Depends on public.vw_post_details (Views/vw_post_details.sql must be
-- deployed first).

DROP FUNCTION IF EXISTS public.fn_post_get_all();

CREATE FUNCTION public.fn_post_get_all()
RETURNS SETOF public.vw_post_details
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.vw_post_details ORDER BY id;
$$;
