/*
    Table:   public.poll_options
    Purpose: Backing store for MvcBB.Shared.Models.Poll.PollOption /
             IPollRepository.
*/

DROP TABLE IF EXISTS public.poll_options CASCADE;

CREATE TABLE public.poll_options
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    poll_id         INTEGER         NOT NULL REFERENCES public.polls (id),
    text            VARCHAR(200)    NOT NULL,
    sort_order      INTEGER         NOT NULL DEFAULT 0
);

CREATE INDEX ix_poll_options_poll_id ON public.poll_options (poll_id);
