-- Matches IUserRepository.ExistsWithEmail(string email, int? excludeUserId = null)

DROP FUNCTION IF EXISTS public.fn_user_exists_with_email(citext, integer);

CREATE FUNCTION public.fn_user_exists_with_email(p_email CITEXT, p_exclude_user_id INTEGER DEFAULT NULL)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1 FROM public.users
        WHERE email = p_email
          AND (p_exclude_user_id IS NULL OR id <> p_exclude_user_id)
    );
$$;
