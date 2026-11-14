/*
    Table:   public.poll_votes
    Purpose: Backing store for MvcBB.Shared.Models.Poll.PollVote /
             IPollRepository - one row per (poll, user) vote.
    Notes:
      - UNIQUE on (poll_id, user_id) enforces single-choice voting at the DB
        level too, matching IPollRepository.AddVote's HasVoted guard.
*/

CREATE TABLE IF NOT EXISTS public.poll_votes
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    poll_id         INTEGER         NOT NULL REFERENCES public.polls (id),
    poll_option_id  INTEGER         NOT NULL REFERENCES public.poll_options (id),
    user_id         INTEGER         NOT NULL REFERENCES public.users (id),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    CONSTRAINT uq_poll_votes_poll_user UNIQUE (poll_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_poll_votes_poll_id ON public.poll_votes (poll_id);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.poll_votes ADD COLUMN IF NOT EXISTS new_column ...;
