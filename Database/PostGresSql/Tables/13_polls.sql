/*
    Table:   public.polls
    Purpose: Backing store for MvcBB.Shared.Models.Poll.Poll / IPollRepository.
    Notes:
      - One poll per thread (UNIQUE on thread_id) - polls are attached only
        at thread-creation time and can't be added retroactively.
*/

CREATE TABLE IF NOT EXISTS public.polls
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    thread_id       INTEGER         NOT NULL UNIQUE REFERENCES public.threads (id),
    question        VARCHAR(200)    NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now()
);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.polls ADD COLUMN IF NOT EXISTS new_column ...;
