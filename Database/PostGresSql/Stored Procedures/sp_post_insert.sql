-- Matches IPostRepository.Add(Post post)
-- Only base posts columns are accepted/returned; the joined presentation
-- fields (thread_title, created_by_username, etc.) are read back via
-- fn_post_get_by_id / public.vw_post_details after insert.

DROP PROCEDURE IF EXISTS public.sp_post_insert(text, integer, integer, integer);

CREATE PROCEDURE public.sp_post_insert(
    p_content TEXT,
    p_thread_id INTEGER,
    p_created_by_user_id INTEGER,
    p_quoted_post_id INTEGER DEFAULT NULL,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.posts (content, thread_id, created_by_user_id, quoted_post_id)
    VALUES (p_content, p_thread_id, p_created_by_user_id, p_quoted_post_id)
    RETURNING id INTO p_id;
END;
$$;
