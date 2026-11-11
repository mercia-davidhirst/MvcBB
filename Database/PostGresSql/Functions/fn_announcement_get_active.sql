-- Matches IAnnouncementManagementService.GetActiveAnnouncements()

DROP FUNCTION IF EXISTS public.fn_announcement_get_active();

CREATE FUNCTION public.fn_announcement_get_active()
RETURNS SETOF public.announcements
LANGUAGE sql
STABLE
AS $$
    SELECT * FROM public.announcements
    WHERE is_active = TRUE
        AND (start_date IS NULL OR start_date <= now())
        AND (end_date IS NULL OR end_date >= now())
    ORDER BY sort_order;
$$;
