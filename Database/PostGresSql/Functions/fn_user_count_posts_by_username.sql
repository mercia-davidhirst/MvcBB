-- Matches IUserRepository.CountPostsByUsername(string username)
-- posts.created_by_user_id stores the author's numeric users.id (see
-- ClaimTypes.NameIdentifier = user.Id.ToString() in UsersController), so the
-- username parameter is resolved via a join rather than a direct column match.
-- Soft-deleted posts (is_deleted = true) are excluded from the count.

DROP FUNCTION IF EXISTS public.fn_user_count_posts_by_username(citext);

CREATE FUNCTION public.fn_user_count_posts_by_username(p_username CITEXT)
RETURNS INTEGER
LANGUAGE sql
STABLE
AS $$
    SELECT COUNT(*)::INTEGER
    FROM public.posts p
    INNER JOIN public.users u ON u.id = p.created_by_user_id
    WHERE u.username = p_username
      AND p.is_deleted = FALSE;
$$;
