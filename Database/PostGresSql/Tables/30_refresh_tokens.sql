/*
    Table:   public.refresh_tokens
    Purpose: Backing store for MvcBB.Shared.Models.User.RefreshToken /
             IRefreshTokenRepository - "Remember me" sign-ins. Added in v1.4.1.
    Notes:
      - Only token_hash (a SHA-256 hash, hex-encoded = 64 chars) is stored.
        The raw token exists only in the member's App cookie, so a database
        leak can't be used to sign in as anyone.
      - Tokens are single-use: refreshing marks one used (used_at) and issues
        its replacement with the same family_id (one family = one sign-in on
        one device). A used token presented again revokes its whole family.
      - session_stamp is the account's session stamp when the token was
        issued; a password, role or lock change makes it stale.
      - revoked_at is set on sign-out (that family), and on a password
        change or reset, lock, role change or anonymization (all of the
        member's tokens).
      - Expired tokens, and used or revoked ones over a week old, are
        deleted daily by the API.
*/

CREATE TABLE IF NOT EXISTS public.refresh_tokens
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id         INTEGER         NOT NULL REFERENCES public.users (id) ON DELETE CASCADE,
    token_hash      CHAR(64)        NOT NULL,
    family_id       UUID            NOT NULL,
    session_stamp   VARCHAR(64)     NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    expires_at      TIMESTAMPTZ     NOT NULL,
    used_at         TIMESTAMPTZ     NULL,
    revoked_at      TIMESTAMPTZ     NULL,
    created_ip      VARCHAR(45)     NULL,
    CONSTRAINT uq_refresh_tokens_token_hash UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON public.refresh_tokens (user_id);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_family_id ON public.refresh_tokens (family_id);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_expires_at ON public.refresh_tokens (expires_at);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here.
