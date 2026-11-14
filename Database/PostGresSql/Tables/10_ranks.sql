/*
    Table:   public.ranks
    Purpose: Backing store for MvcBB.Shared.Models.Rank.RankModel /
             IRankRepository.
    Notes:
      - A user's rank is never stored against them - RankCalculator.GetRank
        computes it on the fly from their live post count against
        min_posts, so it always stays in sync with reality.
*/

CREATE TABLE IF NOT EXISTS public.ranks
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title           VARCHAR(70)     NOT NULL,
    min_posts       INTEGER         NOT NULL DEFAULT 0,
    image_path      VARCHAR(500)    NULL,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    sort_order      INTEGER         NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS ix_ranks_min_posts ON public.ranks (min_posts);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.ranks ADD COLUMN IF NOT EXISTS new_column ...;
