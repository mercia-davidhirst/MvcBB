/*
    Table:   public.moderation_queue
    Purpose: Backing store for MvcBB.Shared.Models.Moderation.ModerationQueueItem /
             IModerationQueueRepository - new members' posts held for a
             moderator's approval (see public.moderation_settings). Added in
             v1.4.0.
    Notes:
      - A held post isn't a thread or post yet, so nothing else in the forum
        (counts, search, last post) ever sees it. Approving it creates the
        thread or reply then; either way the row is deleted afterwards, and
        public.moderation_log keeps the record of the decision.
      - kind mirrors QueuedPostKind: 1 = NewThread (title, board_id and an
        optional poll_json), 2 = Reply (thread_id, optional quoted_post_id).
      - board_id and thread_id have no foreign keys: if the board or thread
        goes before a moderator gets to it, approving fails with a message
        and the moderator can turn it down instead.
      - Rows go with their member (ON DELETE CASCADE).
*/

CREATE TABLE IF NOT EXISTS public.moderation_queue
(
    id                  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    kind                SMALLINT        NOT NULL,
    user_id             INTEGER         NOT NULL REFERENCES public.users (id) ON DELETE CASCADE,
    board_id            INTEGER         NOT NULL,
    thread_id           INTEGER         NULL,
    title               VARCHAR(200)    NULL,
    content             TEXT            NOT NULL,
    quoted_post_id      INTEGER         NULL,
    image_url           VARCHAR(500)    NULL,
    poll_json           TEXT            NULL,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT now(),
    CONSTRAINT ck_moderation_queue_kind CHECK ((kind = 1 AND title IS NOT NULL) OR (kind = 2 AND thread_id IS NOT NULL))
);

CREATE INDEX IF NOT EXISTS ix_moderation_queue_user_id ON public.moderation_queue (user_id);

-- Schema changes for a database that already has an older version of this
-- table: none pending yet - add guarded ALTER TABLE statements here.
