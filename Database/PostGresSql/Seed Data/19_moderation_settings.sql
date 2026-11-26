/*
    Seeds public.moderation_settings with its one singleton row, matching
    MvcBB.Shared.Models.Moderation.ModerationSettings' own defaults (holding
    new members' posts is off). Only runs if the table is still empty, so
    re-running this script won't overwrite values an admin has since changed
    on the Moderation settings page.
*/

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.moderation_settings) THEN
        INSERT INTO public.moderation_settings (id, hold_new_member_posts, new_member_post_threshold)
        VALUES (1, FALSE, 1);
    END IF;
END;
$$;
