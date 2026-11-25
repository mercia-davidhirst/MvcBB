/*
    Table:   public.thread_reads
    Purpose: Backing store for
             MvcBB.Shared.Models.ThreadRead.ThreadReadModel /
             IThreadReadRepository - how far each member has read each
             discussion, so it can open at their first unread post.
    Notes:
      - One row per (thread_id, user_id), created the first time they scroll
        a post into view. Guests aren't tracked.
      - last_read_post_at is the created_at of the newest post they've seen;
        posts are shown oldest first, so anything newer is unread. It only
        ever moves forward (see sp_thread_read_mark_read).
      - last_read_post_id is kept for reference only and has no foreign key,
        so deleting that post never blocks anything or loses the position.
      - Rows go with their thread or member (ON DELETE CASCADE).
*/

CREATE TABLE IF NOT EXISTS public.thread_reads
(
    thread_id               INTEGER         NOT NULL REFERENCES public.threads (id) ON DELETE CASCADE,
    user_id                 INTEGER         NOT NULL REFERENCES public.users (id) ON DELETE CASCADE,
    last_read_post_id       INTEGER         NOT NULL,
    last_read_post_at       TIMESTAMPTZ     NOT NULL,
    updated_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    CONSTRAINT pk_thread_reads PRIMARY KEY (user_id, thread_id)
);

CREATE INDEX IF NOT EXISTS ix_thread_reads_thread_id ON public.thread_reads (thread_id);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.thread_reads ADD COLUMN IF NOT EXISTS new_column ...;
