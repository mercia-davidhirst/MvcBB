-- Matches IModerationQueueRepository.Remove(int id)

DROP PROCEDURE IF EXISTS public.sp_moderation_queue_delete(integer);

CREATE PROCEDURE public.sp_moderation_queue_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.moderation_queue WHERE id = p_id;
END;
$$;
