-- Matches IPostRepository.GetByUserId(int userId, int take) - a member's
-- profile page needs only their few most recent posts, so this filters,
-- sorts and limits entirely in SQL instead of the caller loading every post
-- in the table first (see fn_post_get_all).

DROP FUNCTION IF EXISTS public.fn_post_get_by_user_id(integer, integer);

CREATE FUNCTION public.fn_post_get_by_user_id(p_user_id INTEGER, p_take INTEGER)
RETURNS SETOF public.vw_post_details
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.vw_post_details
    WHERE created_by_user_id = p_user_id AND is_deleted = FALSE
    ORDER BY created_at DESC
    LIMIT p_take;
$$;
