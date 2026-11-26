-- Matches IPostRepository.GetThreadPage(int threadId, int skip, int take)
-- One page of a discussion, in order (created_at, then id). The page's post
-- ids are picked from public.posts first (ix_posts_thread_id_created_at), so
-- the view's per-row work is only done for the posts on this page.
-- Depends on public.vw_post_details. Added in v1.4.0.

DROP FUNCTION IF EXISTS public.fn_post_get_thread_page(integer, integer, integer);

CREATE FUNCTION public.fn_post_get_thread_page(p_thread_id INTEGER, p_skip INTEGER, p_take INTEGER)
RETURNS SETOF public.vw_post_details
LANGUAGE sql
STABLE
AS $$
    SELECT v.*
    FROM (
        SELECT id, created_at
        FROM public.posts
        WHERE thread_id = p_thread_id
        ORDER BY created_at, id
        OFFSET p_skip LIMIT p_take
    ) AS page_posts
    INNER JOIN public.vw_post_details AS v ON v.id = page_posts.id
    ORDER BY page_posts.created_at, page_posts.id;
$$;
