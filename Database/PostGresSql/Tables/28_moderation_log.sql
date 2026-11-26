/*
    Table:   public.moderation_log
    Purpose: Backing store for MvcBB.Shared.Models.Moderation.ModerationLogEntry /
             IModerationLogRepository - every moderator action (approving or
             turning down held posts, deleting and editing posts, locking,
             pinning and deleting threads, locking members, changing roles,
             updating reports). Added in v1.4.0.
    Notes:
      - Written by the API as each action happens; the site never edits or
        deletes rows.
      - actor_user_id and target_user_id have no foreign keys, so the log
        keeps its history even if an account is later removed. summary is
        written at the time (e.g. the thread's title then), so it still reads
        correctly after the thread is renamed or deleted.
*/

CREATE TABLE IF NOT EXISTS public.moderation_log
(
    id                  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    actor_user_id       INTEGER         NOT NULL,
    action              VARCHAR(50)     NOT NULL,
    target_type         VARCHAR(20)     NOT NULL,
    target_id           INTEGER         NULL,
    target_user_id      INTEGER         NULL,
    summary             VARCHAR(500)    NOT NULL,
    reason              VARCHAR(500)    NULL,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_moderation_log_created_at ON public.moderation_log (created_at DESC);

-- Schema changes for a database that already has an older version of this
-- table: none pending yet - add guarded ALTER TABLE statements here.
