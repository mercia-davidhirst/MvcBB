/*
    Table:   public.threads
    Purpose: Backing store for MvcBB.Shared.Models.ForumThread.ForumThread /
             IForumThreadRepository.
    Notes:
      - view_count, post_count, last_post_at and last_post_by_user_id are
        maintained by the application layer, matching
        ThreadsController/PostsController.
      - ix_threads_board_listing supports IForumThreadRepository.GetByBoardId,
        which orders by is_sticky desc, then (last_post_at ?? created_at) desc.
*/

CREATE TABLE IF NOT EXISTS public.threads
(
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title                   VARCHAR(200)    NOT NULL,
    board_id                INTEGER         NOT NULL REFERENCES public.boards (id),
    created_by_user_id      INTEGER         NOT NULL REFERENCES public.users (id),
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ     NULL,
    is_sticky               BOOLEAN         NOT NULL DEFAULT FALSE,
    is_locked               BOOLEAN         NOT NULL DEFAULT FALSE,
    view_count              INTEGER         NOT NULL DEFAULT 0,
    post_count              INTEGER         NOT NULL DEFAULT 0,
    total_likes             INTEGER         NOT NULL DEFAULT 0,
    participant_count       INTEGER         NOT NULL DEFAULT 0,
    last_post_at            TIMESTAMPTZ     NULL,
    last_post_by_user_id    INTEGER         NULL REFERENCES public.users (id),
    is_imported_data        BOOLEAN         NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS ix_threads_board_listing ON public.threads (board_id, is_sticky DESC, last_post_at DESC, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_threads_created_by_user_id ON public.threads (created_by_user_id);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
ALTER TABLE public.threads ADD COLUMN IF NOT EXISTS total_likes INTEGER NOT NULL DEFAULT 0;
ALTER TABLE public.threads ADD COLUMN IF NOT EXISTS participant_count INTEGER NOT NULL DEFAULT 0;
ALTER TABLE public.threads ADD COLUMN IF NOT EXISTS is_imported_data BOOLEAN NOT NULL DEFAULT FALSE;

-- One-time backfill from the data these columns replace - matches what
-- ThreadsController.MapToThreadResponses used to compute on the fly. Cheap
-- to run again on an already-backfilled database (every row already matches).
UPDATE public.threads AS t
SET total_likes = (
    SELECT COUNT(*)
    FROM public.posts AS p
    INNER JOIN public.post_likes AS pl ON pl.post_id = p.id
    WHERE p.thread_id = t.id
);
UPDATE public.threads AS t
SET participant_count = (
    SELECT COUNT(DISTINCT p.created_by_user_id)
    FROM public.posts AS p
    WHERE p.thread_id = t.id
);
