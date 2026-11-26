-- Matches IForumThreadRepository.Remove(ForumThread thread)
-- Deletes the thread and everything that belongs to it (a procedure call
-- runs in one transaction): its posts and their likes, its poll (options and
-- votes), watchers, read positions (ON DELETE CASCADE), notifications about
-- it and any replies to it still waiting in the moderation queue. A post in
-- another thread that quoted one of these posts keeps its own text but loses
-- the quote link (quoted_post_id set to NULL).
-- Before v1.4.0 this only deleted the thread row, which failed with a
-- foreign key error for any thread that had posts.

DROP PROCEDURE IF EXISTS public.sp_thread_delete(integer);

CREATE PROCEDURE public.sp_thread_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.posts
    SET quoted_post_id = NULL
    WHERE thread_id <> p_id
      AND quoted_post_id IN (SELECT id FROM public.posts WHERE thread_id = p_id);

    DELETE FROM public.post_likes
    WHERE post_id IN (SELECT id FROM public.posts WHERE thread_id = p_id);

    DELETE FROM public.posts WHERE thread_id = p_id;

    DELETE FROM public.poll_votes
    WHERE poll_id IN (SELECT id FROM public.polls WHERE thread_id = p_id);

    DELETE FROM public.poll_options
    WHERE poll_id IN (SELECT id FROM public.polls WHERE thread_id = p_id);

    DELETE FROM public.polls WHERE thread_id = p_id;
    DELETE FROM public.thread_subscriptions WHERE thread_id = p_id;
    DELETE FROM public.notifications WHERE thread_id = p_id;
    DELETE FROM public.moderation_queue WHERE thread_id = p_id;

    DELETE FROM public.threads WHERE id = p_id;
END;
$$;
