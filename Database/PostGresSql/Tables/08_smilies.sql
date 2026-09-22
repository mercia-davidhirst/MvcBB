/*
    Table:   public.smilies
    Purpose: Backing store for MvcBB.Shared.Models.BBCode.SmilieModel /
             IBBCodeManagementService's smilie methods (GetSmilies,
             GetSmilie, AddSmilie, UpdateSmilie, DeleteSmilie).
    Notes:
      - Same in-memory situation as bb_code_tags: MvcBB.App.Services.BBCodeService
        seeds this list from a hardcoded set in its constructor; see
        Seed Data/08_smilies.sql for the equivalent seed data.
      - code is unique (":)"/":-)" etc are each their own distinct code in the
        current seed list, but two rows sharing the same code would be
        ambiguous for GetAvailableSmilies' dictionary lookup).
*/

DROP TABLE IF EXISTS public.smilies CASCADE;

CREATE TABLE public.smilies
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code            VARCHAR(20)     NOT NULL,
    description     VARCHAR(200)    NOT NULL DEFAULT '',
    image_path      VARCHAR(500)    NOT NULL DEFAULT '',
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    sort_order      INTEGER         NOT NULL DEFAULT 0,
    CONSTRAINT uq_smilies_code UNIQUE (code)
);

CREATE INDEX ix_smilies_sort_order ON public.smilies (sort_order);
