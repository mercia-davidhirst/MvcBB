/*
    Table:   public.messages
    Purpose: Backing store for MvcBB.Shared.Models.Message.Message /
             IMessageRepository.
    Notes:
      - ix_messages_recipient supports GetByRecipientUserId(unreadOnly) and
        GetUnreadCountByRecipientUserId, both filtered/sorted by recipient +
        read state + recency.
*/

CREATE TABLE IF NOT EXISTS public.messages
(
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    subject                 VARCHAR(200)    NOT NULL DEFAULT '',
    content                 TEXT            NOT NULL,
    sender_user_id          INTEGER         NOT NULL REFERENCES public.users (id),
    recipient_user_id       INTEGER         NOT NULL REFERENCES public.users (id),
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    read_at                 TIMESTAMPTZ     NULL
);

CREATE INDEX IF NOT EXISTS ix_messages_recipient ON public.messages (recipient_user_id, created_at DESC) INCLUDE (read_at);
CREATE INDEX IF NOT EXISTS ix_messages_sender ON public.messages (sender_user_id, created_at DESC);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   ALTER TABLE public.messages ADD COLUMN IF NOT EXISTS new_column ...;
