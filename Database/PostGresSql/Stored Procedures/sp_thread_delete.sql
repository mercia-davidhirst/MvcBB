-- Matches IForumThreadRepository.Remove(ForumThread thread)

DROP PROCEDURE IF EXISTS public.sp_thread_delete(integer);

CREATE PROCEDURE public.sp_thread_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.threads WHERE id = p_id;
END;
$$;
