/*
    Table:   public.boards
    Purpose: Backing store for MvcBB.Shared.Models.Board.Board / IBoardRepository.
    Notes:
      - thread_count, post_count, last_post_at and last_post_by_user_id are
        maintained by the application layer (see ThreadsController.CreateThread
        / DeleteThread, which read-modify-write these fields directly), not by
        database triggers, so they are plain columns here.
      - parent_id is a self-referencing FK supporting WBB-style nested board
        categories (arbitrary depth); NULL means a top-level board. is_category
        marks a pure organizational header with no threads of its own.
*/

DROP TABLE IF EXISTS public.boards CASCADE;

CREATE TABLE public.boards
(
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name                    VARCHAR(100)    NOT NULL,
    description             VARCHAR(500)    NOT NULL DEFAULT '',
    sort_order              INTEGER         NOT NULL DEFAULT 0,
    is_active               BOOLEAN         NOT NULL DEFAULT TRUE,
    parent_id               INTEGER         NULL REFERENCES public.boards (id),
    is_category             BOOLEAN         NOT NULL DEFAULT FALSE,
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    updated_at              TIMESTAMPTZ     NULL,
    thread_count            INTEGER         NOT NULL DEFAULT 0,
    post_count              INTEGER         NOT NULL DEFAULT 0,
    last_post_by_user_id    INTEGER         NULL REFERENCES public.users (id),
    last_post_at            TIMESTAMPTZ     NULL
);

CREATE INDEX ix_boards_sort_order ON public.boards (sort_order);

CREATE INDEX ix_boards_parent_id ON public.boards (parent_id);
