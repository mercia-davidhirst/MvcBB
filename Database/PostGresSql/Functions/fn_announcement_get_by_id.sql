-- Matches IAnnouncementManagementService.GetAnnouncement(int id)

DROP FUNCTION IF EXISTS public.fn_announcement_get_by_id(integer);

CREATE FUNCTION public.fn_announcement_get_by_id(p_id INTEGER)
RETURNS SETOF public.announcements
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.announcements WHERE id = p_id;
$$;
