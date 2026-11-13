-- Matches IPostRepository.Search(string query, int? boardId, bool
-- includePrivateBoards, int skip, int take) - filters by content/author
-- username, an optional board (via each post's thread) and board
-- visibility, then paginates entirely in SQL. The threads/boards join
-- exists only to filter; the returned columns match vw_post_details
-- exactly, same shape as the other fn_post_get_* functions - board id/name
-- for display come from a small batched IForumThreadRepository.GetByIds
-- call afterwards, since ForumThread already carries them denormalized.

DROP FUNCTION IF EXISTS public.fn_search_posts(text, integer, boolean, integer, integer);

CREATE FUNCTION public.fn_search_posts(
    p_query TEXT,
    p_board_id INTEGER,
    p_include_private_boards BOOLEAN,
    p_skip INTEGER,
    p_take INTEGER
)
RETURNS SETOF public.vw_post_details
LANGUAGE sql
STABLE
AS $$
    SELECT pd.*
    FROM public.vw_post_details AS pd
    INNER JOIN public.threads AS th ON th.id = pd.thread_id
    INNER JOIN public.boards AS b ON b.id = th.board_id
    WHERE (pd.content ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\'
           OR pd.created_by_username ILIKE '%' || REPLACE(REPLACE(REPLACE(p_query, '\', '\\'), '%', '\%'), '_', '\_') || '%' ESCAPE '\')
      AND (p_board_id IS NULL OR b.id = p_board_id)
      AND (p_include_private_boards OR NOT b.is_private)
    ORDER BY pd.created_at DESC
    OFFSET p_skip
    LIMIT p_take;
$$;
