-- Matches IUserRepository.GetById(int id)

DROP FUNCTION IF EXISTS public.fn_user_get_by_id(integer);

CREATE FUNCTION public.fn_user_get_by_id(p_id INTEGER)
RETURNS SETOF public.users
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.users WHERE id = p_id;
$$;
