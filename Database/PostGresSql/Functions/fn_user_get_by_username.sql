-- Matches IUserRepository.GetByUsername(string username)
-- Equality against the citext username column is case-insensitive already,
-- matching the app's StringComparison.OrdinalIgnoreCase semantics.

DROP FUNCTION IF EXISTS public.fn_user_get_by_username(citext);

CREATE FUNCTION public.fn_user_get_by_username(p_username CITEXT)
RETURNS SETOF public.users
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.users WHERE username = p_username;
$$;
