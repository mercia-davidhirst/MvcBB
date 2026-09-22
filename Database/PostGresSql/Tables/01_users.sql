/*
    Table:   public.users
    Purpose: Backing store for MvcBB.Shared.Models.User.User / IUserRepository.
    Notes:
      - username/email are CITEXT so equality and the unique constraints are
        case-insensitive (see 00_extensions.sql), matching GetByUsername,
        GetByEmail, ExistsWithUsername and ExistsWithEmail in IUserRepository.
      - thread_count/post_count on the User model are NOT persisted here: the
        repository exposes CountPostsByUsername/CountThreadsByUsername as
        computed queries (see fn_user_count_posts_by_username /
        fn_user_count_threads_by_username), so a running total would just
        duplicate derivable data.
      - role mirrors MvcBB.Shared.Models.User.UserRole: 0 = User,
        1 = Moderator, 2 = Administrator.
      - CASCADE on the drop removes any dependent foreign keys/views so this
        script can be re-run standalone regardless of deployment order.
*/

DROP TABLE IF EXISTS public.users CASCADE;

CREATE TABLE public.users
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username        CITEXT          NOT NULL,
    password_hash   VARCHAR(256)    NOT NULL,
    email           CITEXT          NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    last_login_at   TIMESTAMPTZ     NULL,
    role            SMALLINT        NOT NULL DEFAULT 0,
    signature       VARCHAR(1000)   NULL,
    bio             VARCHAR(2000)   NULL,
    avatar_url      VARCHAR(500)    NULL,
    show_email      BOOLEAN         NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_users_username UNIQUE (username),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT ck_users_role CHECK (role IN (0, 1, 2))
);
