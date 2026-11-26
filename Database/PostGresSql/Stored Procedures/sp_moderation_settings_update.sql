-- Matches IModerationSettingsRepository.Update(ModerationSettings settings)
-- Creates the singleton row if the seed script hasn't run yet.

DROP PROCEDURE IF EXISTS public.sp_moderation_settings_update(boolean, integer);

CREATE PROCEDURE public.sp_moderation_settings_update(
    p_hold_new_member_posts BOOLEAN,
    p_new_member_post_threshold INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.moderation_settings (id, hold_new_member_posts, new_member_post_threshold)
    VALUES (1, p_hold_new_member_posts, p_new_member_post_threshold)
    ON CONFLICT (id) DO UPDATE
        SET hold_new_member_posts = EXCLUDED.hold_new_member_posts,
            new_member_post_threshold = EXCLUDED.new_member_post_threshold;
END;
$$;
