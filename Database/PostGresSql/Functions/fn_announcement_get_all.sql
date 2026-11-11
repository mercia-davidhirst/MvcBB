-- Matches IAnnouncementManagementService.GetAnnouncements()

DROP FUNCTION IF EXISTS public.fn_announcement_get_all();

CREATE FUNCTION public.fn_announcement_get_all()
RETURNS SETOF public.announcements
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.announcements ORDER BY sort_order;
$$;
