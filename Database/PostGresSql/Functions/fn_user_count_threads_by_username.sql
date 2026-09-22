-- Matches IUserRepository.CountThreadsByUsername(string username)
-- threads.created_by_user_id stores the author's numeric users.id, same as
-- posts (see fn_user_count_posts_by_username for the reasoning).

DROP FUNCTION IF EXISTS public.fn_user_count_threads_by_username(citext);

CREATE FUNCTION public.fn_user_count_threads_by_username(p_username CITEXT)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER
    FROM public.threads t
    INNER JOIN public.users u ON u.id = t.created_by_user_id
    WHERE u.username = p_username;
$$;
