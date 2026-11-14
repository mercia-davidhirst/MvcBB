/*
    Table:   public.poll_options
    Purpose: Backing store for MvcBB.Shared.Models.Poll.PollOption /
             IPollRepository.
*/

CREATE TABLE IF NOT EXISTS public.poll_options
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    poll_id         INTEGER         NOT NULL REFERENCES public.polls (id),
    text            VARCHAR(200)    NOT NULL,
    sort_order      INTEGER         NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_poll_options_poll_id ON public.poll_options (poll_id);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.poll_options ADD COLUMN IF NOT EXISTS new_column ...;
