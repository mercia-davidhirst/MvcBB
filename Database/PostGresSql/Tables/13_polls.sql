/*
    Table:   public.polls
    Purpose: Backing store for MvcBB.Shared.Models.Poll.Poll / IPollRepository.
    Notes:
      - One poll per thread (UNIQUE on thread_id) - polls are attached only
        at thread-creation time and can't be added retroactively.
*/

DROP TABLE IF EXISTS public.polls CASCADE;

CREATE TABLE public.polls
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    thread_id       INTEGER         NOT NULL UNIQUE REFERENCES public.threads (id),
    question        VARCHAR(200)    NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now()
);
