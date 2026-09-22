/*
    Table:   public.posts
    Purpose: Backing store for MvcBB.Shared.Models.Post.Post / IPostRepository.
    Notes:
      - content is TEXT (Postgres has no practical length cap on varchar
        anyway); the model's 10000-character limit is enforced with a CHECK
        constraint instead.
      - thread_title, created_by_username, updated_by_username,
        deleted_by_username, user_avatar, user_signature, user_post_count,
        user_joined_at and user_role on the Post model are all join/aggregate
        -derived presentation fields, not stored columns here - see
        public.vw_post_details, which reconstructs them.
      - quoted_post_id is a self-reference; ON DELETE/UPDATE is left as the
        default (NO ACTION) to avoid multiple cascade paths back into
        public.posts.
*/

DROP TABLE IF EXISTS public.posts CASCADE;

CREATE TABLE public.posts
(
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    content                 TEXT            NOT NULL,
    thread_id               INTEGER         NOT NULL REFERENCES public.threads (id),
    created_by_user_id      INTEGER         NOT NULL REFERENCES public.users (id),
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    is_edited               BOOLEAN         NOT NULL DEFAULT FALSE,
    edit_reason             VARCHAR(500)    NULL,
    updated_at               TIMESTAMPTZ    NULL,
    updated_by_user_id      INTEGER         NULL REFERENCES public.users (id),
    is_deleted               BOOLEAN        NOT NULL DEFAULT FALSE,
    deleted_by_user_id      INTEGER         NULL REFERENCES public.users (id),
    deleted_at               TIMESTAMPTZ    NULL,
    delete_reason           VARCHAR(500)    NULL,
    quoted_post_id          INTEGER         NULL REFERENCES public.posts (id),
    CONSTRAINT ck_posts_content_length CHECK (char_length(content) <= 10000)
);

CREATE INDEX ix_posts_thread_id_created_at ON public.posts (thread_id, created_at);
CREATE INDEX ix_posts_created_by_user_id ON public.posts (created_by_user_id) INCLUDE (is_deleted);
