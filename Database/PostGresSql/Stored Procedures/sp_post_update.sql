-- Matches IPostRepository.Update(Post post)

DROP PROCEDURE IF EXISTS public.sp_post_update(integer, text, boolean, varchar, integer, boolean, integer, timestamptz, varchar, integer);

CREATE PROCEDURE public.sp_post_update(
    p_id INTEGER,
    p_content TEXT,
    p_is_edited BOOLEAN,
    p_edit_reason VARCHAR(500) DEFAULT NULL,
    p_updated_by_user_id INTEGER DEFAULT NULL,
    p_is_deleted BOOLEAN DEFAULT FALSE,
    p_deleted_by_user_id INTEGER DEFAULT NULL,
    p_deleted_at TIMESTAMPTZ DEFAULT NULL,
    p_delete_reason VARCHAR(500) DEFAULT NULL,
    p_quoted_post_id INTEGER DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.posts
    SET content = p_content,
        is_edited = p_is_edited,
        edit_reason = p_edit_reason,
        updated_at = now(),
        updated_by_user_id = p_updated_by_user_id,
        is_deleted = p_is_deleted,
        deleted_by_user_id = p_deleted_by_user_id,
        deleted_at = p_deleted_at,
        delete_reason = p_delete_reason,
        quoted_post_id = p_quoted_post_id
    WHERE id = p_id;
END;
$$;
