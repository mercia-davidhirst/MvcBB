/*
    Table:   public.announcements
    Purpose: Backing store for MvcBB.Shared.Models.Announcement.AnnouncementModel /
             IAnnouncementManagementService (GetAnnouncements, GetActiveAnnouncements,
             GetAnnouncement, AddAnnouncement, UpdateAnnouncement, DeleteAnnouncement).
    Notes:
      - start_date/end_date are optional scheduling bounds - NULL means "no
        lower/upper bound". fn_announcement_get_active is what the portal page
        and any other public surface should call, since it already applies
        is_active plus the start_date/end_date window.
*/

DROP TABLE IF EXISTS public.announcements CASCADE;

CREATE TABLE public.announcements
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title           VARCHAR(200)    NOT NULL,
    content         TEXT            NOT NULL,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    sort_order      INTEGER         NOT NULL DEFAULT 0,
    start_date      TIMESTAMPTZ     NULL,
    end_date        TIMESTAMPTZ     NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX ix_announcements_sort_order ON public.announcements (sort_order);
