/*
    Table:   public.bb_code_tags
    Purpose: Backing store for MvcBB.Shared.Models.BBCode.BBCodeTagModel /
             IBBCodeManagementService's tag methods (GetBBCodeTags,
             GetBBCodeTag, AddBBCodeTag, UpdateBBCodeTag, DeleteBBCodeTag).
    Notes:
      - MvcBB.App.Services.BBCodeService currently holds this list entirely
        in-memory, seeded from a hardcoded set in its constructor. This table
        is where that data belongs once the service is backed by a
        repository; see Seed Data/07_bb_code_tags.sql for the equivalent of
        that hardcoded seed list.
      - name is unique so the admin tag list (Admin/BBCode) can't end up with
        two ambiguous entries sharing a display name.
*/

CREATE TABLE IF NOT EXISTS public.bb_code_tags
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    pattern         VARCHAR(500)    NOT NULL,
    replacement     VARCHAR(1000)   NOT NULL,
    description     VARCHAR(500)    NOT NULL DEFAULT '',
    example         VARCHAR(500)    NOT NULL DEFAULT '',
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    sort_order      INTEGER         NOT NULL DEFAULT 0,
    CONSTRAINT uq_bb_code_tags_name UNIQUE (name)
);

CREATE INDEX IF NOT EXISTS ix_bb_code_tags_sort_order ON public.bb_code_tags (sort_order);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.bb_code_tags ADD COLUMN IF NOT EXISTS new_column ...;
