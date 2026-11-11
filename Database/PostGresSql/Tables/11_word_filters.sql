/*
    Table:   public.word_filters
    Purpose: Backing store for
             MvcBB.Shared.Models.WordFilter.WordFilterModel /
             IWordFilterRepository.
    Notes:
      - Applied once, at write time, to a thread's title and a post's
        content before either is saved (see ThreadsController.CreateThread
        and PostsController.CreatePost/UpdatePost in MvcBB.API) - not at
        render time - so the stored data is already clean.
*/

DROP TABLE IF EXISTS public.word_filters CASCADE;

CREATE TABLE public.word_filters
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    word            VARCHAR(100)    NOT NULL,
    replacement     VARCHAR(100)    NOT NULL DEFAULT '',
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    sort_order      INTEGER         NOT NULL DEFAULT 0,
    CONSTRAINT uq_word_filters_word UNIQUE (word)
);

CREATE INDEX ix_word_filters_sort_order ON public.word_filters (sort_order);
