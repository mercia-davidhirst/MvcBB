/*
    Upgrade: MvcBB v1.4.0 -> v1.4.1 (PostgreSQL)

    Adds "Remember me" sign-in and the settings for the home page "Who's
    online" card. Safe to run any number of times: every change is guarded,
    so a database that already has it (including a fresh v1.4.1 install,
    whose Tables/ scripts create the new shape directly) is left as it is.
    No existing rows are changed or deleted.

    install.ps1 runs this straight after Tables/ and then recreates every
    function and procedure, including the new fn_refresh_token_* and
    sp_refresh_token_* ones, so re-running install.ps1 is the normal way to
    upgrade. To run it on its own (you will still need those functions and
    procedures):
        psql -h <host> -U <user> -d <database> -v ON_ERROR_STOP=1 -f Upgrades/v1.4.1.sql

    1. refresh_tokens - one row per "Remember me" refresh token (stored only
       as a SHA-256 hash). The same definition as
       Tables/30_refresh_tokens.sql.
    2. display_settings.show_whos_online (default on) and
       online_window_minutes (default 15) - set on Admin > Settings.
*/

-- 1. refresh_tokens table.
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

-- 2. display_settings.show_whos_online / online_window_minutes - the home
--    page "Who's online" card (on by default) and how many minutes count as
--    online (default 15).
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS show_whos_online BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS online_window_minutes INTEGER NOT NULL DEFAULT 15;
