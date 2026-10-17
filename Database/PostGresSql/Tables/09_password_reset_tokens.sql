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

DROP TABLE IF EXISTS public.password_reset_tokens CASCADE;

CREATE TABLE public.password_reset_tokens
(
    id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id         INTEGER         NOT NULL REFERENCES public.users (id),
    token_hash      CHAR(64)        NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    expires_at      TIMESTAMPTZ     NOT NULL,
    used_at         TIMESTAMPTZ     NULL,
    CONSTRAINT uq_password_reset_tokens_token_hash UNIQUE (token_hash)
);

CREATE INDEX ix_password_reset_tokens_user_id ON public.password_reset_tokens (user_id);
