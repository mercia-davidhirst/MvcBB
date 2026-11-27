-- Matches IPostRepository.GetThreadLocation(int threadId, int? postId, DateTime? unreadAfter, int? excludeUserId)
-- Returns the discussion's post count and, for p_post_id (or, without it, the
-- first post after p_unread_after not written by p_exclude_user_id), that
-- post's id and 0-based position (post_position) in (created_at, id) order.
-- post_id and post_position are NULL when there's no such post. Added in
-- v1.4.0; the column was "position" until v1.4.1, which PostgreSQL rejects
-- (a reserved word inside PL/pgSQL), so the function failed to install.

DROP FUNCTION IF EXISTS public.fn_post_get_thread_location(integer, integer, timestamptz, integer);

CREATE FUNCTION public.fn_post_get_thread_location(p_thread_id INTEGER, p_post_id INTEGER, p_unread_after TIMESTAMPTZ, p_exclude_user_id INTEGER)
RETURNS TABLE (total_count INTEGER, post_id INTEGER, post_position INTEGER)
LANGUAGE plpgsql
STABLE
AS $$
DECLARE
    v_total INTEGER;
    v_target_id INTEGER;
    v_target_at TIMESTAMPTZ;
BEGIN
    SELECT COUNT(*)::integer INTO v_total FROM public.posts p WHERE p.thread_id = p_thread_id;

    IF p_post_id IS NOT NULL THEN
        SELECT p.id, p.created_at INTO v_target_id, v_target_at
        FROM public.posts p
        WHERE p.id = p_post_id AND p.thread_id = p_thread_id;
    ELSIF p_unread_after IS NOT NULL THEN
        SELECT p.id, p.created_at INTO v_target_id, v_target_at
        FROM public.posts p
        WHERE p.thread_id = p_thread_id
          AND p.created_at > p_unread_after
          AND (p_exclude_user_id IS NULL OR p.created_by_user_id <> p_exclude_user_id)
        ORDER BY p.created_at, p.id
        LIMIT 1;
    END IF;

    RETURN QUERY
    SELECT v_total,
           v_target_id,
           CASE WHEN v_target_id IS NULL THEN NULL ELSE (
               SELECT COUNT(*)::integer
               FROM public.posts p
               WHERE p.thread_id = p_thread_id
                 AND (p.created_at < v_target_at OR (p.created_at = v_target_at AND p.id < v_target_id))
           ) END;
END;
$$;
