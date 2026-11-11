-- Matches IAnnouncementManagementService.DeleteAnnouncement(int id)

DROP PROCEDURE IF EXISTS public.sp_announcement_delete(integer);

CREATE PROCEDURE public.sp_announcement_delete(p_id INTEGER)
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM public.announcements WHERE id = p_id;
END;
$$;
