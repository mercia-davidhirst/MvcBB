-- Matches IModerationLogRepository.Add(ModerationLogEntry entry)

DROP PROCEDURE IF EXISTS public.sp_moderation_log_insert(integer, varchar, varchar, integer, integer, varchar, varchar);

CREATE PROCEDURE public.sp_moderation_log_insert(
    p_actor_user_id INTEGER,
    p_action VARCHAR(50),
    p_target_type VARCHAR(20),
    p_target_id INTEGER,
    p_target_user_id INTEGER,
    p_summary VARCHAR(500),
    p_reason VARCHAR(500)
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.moderation_log (actor_user_id, action, target_type, target_id, target_user_id, summary, reason)
    VALUES (p_actor_user_id, p_action, p_target_type, p_target_id, p_target_user_id, LEFT(p_summary, 500), LEFT(p_reason, 500));
END;
$$;
