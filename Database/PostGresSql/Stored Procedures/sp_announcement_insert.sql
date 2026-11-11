-- Matches IAnnouncementManagementService.AddAnnouncement(AnnouncementModel model)

DROP PROCEDURE IF EXISTS public.sp_announcement_insert(varchar, text, boolean, integer, timestamptz, timestamptz, timestamptz);

CREATE PROCEDURE public.sp_announcement_insert(
    p_title VARCHAR(200),
    p_content TEXT,
    p_is_active BOOLEAN DEFAULT TRUE,
    p_sort_order INTEGER DEFAULT 0,
    p_start_date TIMESTAMPTZ DEFAULT NULL,
    p_end_date TIMESTAMPTZ DEFAULT NULL,
    p_created_at TIMESTAMPTZ DEFAULT now(),
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
