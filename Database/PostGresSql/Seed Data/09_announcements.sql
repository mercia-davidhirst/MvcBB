/*
    Seeds public.announcements with a default welcome announcement, matching
    MvcBB.API.InMemory.InMemoryStore's SeedAnnouncements(). Only runs if the
    table is still empty, so re-running this script won't resurrect a row an
    admin has since edited or deleted via Admin/Announcements.
*/

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public.announcements) THEN
        INSERT INTO public.announcements (title, content, is_active, sort_order, start_date, end_date, created_at)
        VALUES
            ('Welcome to MvcBB!', 'Welcome to the forums :) Please read the rules before posting.', TRUE, 1, NULL, NULL, now());
    END IF;
END;
$$;
