/*
    Table:   public.notifications
    Purpose: Backing store for MvcBB.Shared.Models.Notification.Notification /
             INotificationRepository - each member's notification centre
             (replies, @mentions, quotes, likes, private messages and
             moderation decisions). Added in v1.4.0.
    Notes:
      - type mirrors NotificationType: 1 = Reply, 2 = Mention, 3 = Like,
        4 = Message, 5 = Quote, 6 = Moderation.
      - Only user_id has a foreign key (rows go with their member).
        actor_user_id, thread_id, post_id and message_id are kept for
        reference only: a notification about a thread that has since been
        deleted simply isn't shown (the API checks the thread is still there
        and visible).
      - excerpt is a short plain-text extract written at the time.
      - Read notifications older than Notifications:RetentionDays (API
        appsettings.json, default 90) are deleted daily.
*/

CREATE TABLE IF NOT EXISTS public.notifications
(
    id                  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id             INTEGER         NOT NULL REFERENCES public.users (id) ON DELETE CASCADE,
    type                SMALLINT        NOT NULL CHECK (type BETWEEN 1 AND 6),
    actor_user_id       INTEGER         NULL,
    thread_id           INTEGER         NULL,
    post_id             INTEGER         NULL,
    message_id          INTEGER         NULL,
    excerpt             VARCHAR(300)    NULL,
    is_read             BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_notifications_user_id_created_at ON public.notifications (user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notifications_user_id_unread ON public.notifications (user_id) WHERE is_read = FALSE;
CREATE INDEX IF NOT EXISTS ix_notifications_post_id ON public.notifications (post_id) WHERE post_id IS NOT NULL;

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here.
