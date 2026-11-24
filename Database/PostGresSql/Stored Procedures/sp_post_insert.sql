-- Matches IPostRepository.Add(Post post)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).
-- Only base posts columns are accepted/returned; the joined presentation
-- fields (thread_title, created_by_username, etc.) are read back via
-- fn_post_get_by_id / public.vw_post_details after insert.

DROP PROCEDURE IF EXISTS public.sp_post_insert(text, integer, integer, integer, varchar);

CREATE PROCEDURE public.sp_post_insert(
    p_content TEXT,
    p_thread_id INTEGER,
    p_created_by_user_id INTEGER,
    p_quoted_post_id INTEGER,
    p_image_url VARCHAR(500),
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.posts (content, thread_id, created_by_user_id, quoted_post_id, image_url)
    VALUES (p_content, p_thread_id, p_created_by_user_id, p_quoted_post_id, p_image_url)
    RETURNING id INTO p_id;
END;
$$;
