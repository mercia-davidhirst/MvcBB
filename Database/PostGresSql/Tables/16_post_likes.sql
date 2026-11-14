/*
    Table:   public.post_likes
    Purpose: Backing store for MvcBB.Shared.Interfaces.IPostLikeRepository
             (HasLiked, GetLikeCount, GetLikeCounts, GetLikedPostIds, AddLike,
             RemoveLike). This table did not exist in this folder before -
             PostgreSqlPostLikeRepository already called fn_post_like_* /
             sp_post_like_* against it, but nothing had actually created
             them, so every call would have failed against a database
             deployed only from this folder.
    Notes:
      - Composite primary key (post_id, user_id): a user can like a given
        post at most once, matching HasLiked/AddLike's semantics (AddLike is
        a no-op if the like already exists - see the repositories).
*/

CREATE TABLE IF NOT EXISTS public.post_likes
(
    post_id     INTEGER         NOT NULL REFERENCES public.posts (id),
    user_id     INTEGER         NOT NULL REFERENCES public.users (id),
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT now(),
    PRIMARY KEY (post_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_post_likes_post_id ON public.post_likes (post_id);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.post_likes ADD COLUMN IF NOT EXISTS new_column ...;
