/*
    Table:   public.password_reset_tokens
    Purpose: Backing store for MvcBB.Shared.Models.User.PasswordResetToken /
             IPasswordResetTokenRepository.
    Notes:
      - Only token_hash (a SHA-256 hash, hex-encoded = 64 chars) is ever
        persisted - the raw token only ever exists in the emailed reset link,
        never in the database, so a database leak can't be used to reset
        accounts.
      - A token is single-use: used_at is set once consumed, and
        UsersController.ResetPassword rejects anything already used or past
        expires_at.
*/

CREATE TABLE IF NOT EXISTS public.password_reset_tokens
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id         INTEGER         NOT NULL REFERENCES public.users (id),
    token_hash      CHAR(64)        NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    expires_at      TIMESTAMPTZ     NOT NULL,
    used_at         TIMESTAMPTZ     NULL,
    CONSTRAINT uq_password_reset_tokens_token_hash UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_user_id ON public.password_reset_tokens (user_id);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.password_reset_tokens ADD COLUMN IF NOT EXISTS new_column ...;
