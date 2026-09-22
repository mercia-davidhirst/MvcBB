-- Matches IUserRepository.GetByEmail(string email)

DROP FUNCTION IF EXISTS public.fn_user_get_by_email(citext);

CREATE FUNCTION public.fn_user_get_by_email(p_email CITEXT)
RETURNS SETOF public.users
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.users WHERE email = p_email;
$$;
