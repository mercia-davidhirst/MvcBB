-- Matches IPostRepository.GetByUserId(int userId, int take, bool
-- includePrivateBoards, bool includeInactiveBoards) - a member's profile
-- page needs only their few most recent posts, so this filters, sorts and
-- limits entirely in SQL instead of the caller loading every post in the
-- table first (see fn_post_get_all). The threads/boards join applies the
-- same board rules as the rest of the site: guests never get posts from
-- private boards, and only administrators get posts from inactive boards.

DROP FUNCTION IF EXISTS public.fn_post_get_by_user_id(integer, integer);
DROP FUNCTION IF EXISTS public.fn_post_get_by_user_id(integer, integer, boolean, boolean);

CREATE FUNCTION public.fn_post_get_by_user_id(
    p_user_id INTEGER,
    p_take INTEGER,
    p_include_private_boards BOOLEAN,
    p_include_inactive_boards BOOLEAN
)
RETURNS SETOF public.vw_post_details
LANGUAGE sql
STABLE
AS $$
    SELECT pd.*
    FROM public.vw_post_details AS pd
    INNER JOIN public.threads AS th ON th.id = pd.thread_id
    INNER JOIN public.boards AS b ON b.id = th.board_id
    WHERE pd.created_by_user_id = p_user_id
      AND pd.is_deleted = FALSE
      AND (p_include_private_boards OR NOT b.is_private)
      AND (p_include_inactive_boards OR b.is_active)
    ORDER BY pd.created_at DESC
    LIMIT p_take;
$$;
