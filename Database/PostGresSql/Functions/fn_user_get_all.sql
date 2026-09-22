-- Matches IUserRepository.GetAll()

DROP FUNCTION IF EXISTS public.fn_user_get_all();

CREATE FUNCTION public.fn_user_get_all()
RETURNS SETOF public.users
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.users ORDER BY username;
$$;
