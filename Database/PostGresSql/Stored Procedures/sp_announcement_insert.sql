-- Matches IAnnouncementManagementService.AddAnnouncement(AnnouncementModel model)
-- No parameter defaults: PostgreSQL won't accept OUT p_id after parameters
-- with defaults, so callers pass every argument (NULL for p_id).

DROP PROCEDURE IF EXISTS public.sp_announcement_insert(varchar, text, boolean, integer, timestamptz, timestamptz, timestamptz);

CREATE PROCEDURE public.sp_announcement_insert(
    p_title VARCHAR(200),
    p_content TEXT,
    p_is_active BOOLEAN,
    p_sort_order INTEGER,
    p_start_date TIMESTAMPTZ,
    p_end_date TIMESTAMPTZ,
    p_created_at TIMESTAMPTZ,
    OUT p_id INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.announcements (title, content, is_active, sort_order, start_date, end_date, created_at)
    VALUES (p_title, p_content, p_is_active, p_sort_order, p_start_date, p_end_date, p_created_at)
    RETURNING id INTO p_id;
END;
$$;
