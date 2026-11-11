-- Matches IAnnouncementManagementService.UpdateAnnouncement(int id, AnnouncementModel model)

DROP PROCEDURE IF EXISTS public.sp_announcement_update(integer, varchar, text, boolean, integer, timestamptz, timestamptz);

CREATE PROCEDURE public.sp_announcement_update(
    p_id INTEGER,
    p_title VARCHAR(200),
    p_content TEXT,
    p_is_active BOOLEAN,
    p_sort_order INTEGER,
    p_start_date TIMESTAMPTZ,
    p_end_date TIMESTAMPTZ
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE public.announcements
    SET title = p_title,
        content = p_content,
        is_active = p_is_active,
        sort_order = p_sort_order,
        start_date = p_start_date,
        end_date = p_end_date
    WHERE id = p_id;
END;
$$;
