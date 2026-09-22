-- Matches IUserRepository.ExistsWithUsername(string username, int? excludeUserId = null)

DROP FUNCTION IF EXISTS public.fn_user_exists_with_username(citext, integer);

CREATE FUNCTION public.fn_user_exists_with_username(p_username CITEXT, p_exclude_user_id INTEGER DEFAULT NULL)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1 FROM public.users
        WHERE username = p_username
          AND (p_exclude_user_id IS NULL OR id <> p_exclude_user_id)
    );
$$;
