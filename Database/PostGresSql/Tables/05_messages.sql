/*
    Table:   public.messages
    Purpose: Backing store for MvcBB.Shared.Models.Message.Message /
             IMessageRepository.
    Notes:
      - ix_messages_recipient supports GetByRecipientUserId(unreadOnly) and
        GetUnreadCountByRecipientUserId, both filtered/sorted by recipient +
        read state + recency.
*/

DROP TABLE IF EXISTS public.messages CASCADE;

CREATE TABLE public.messages
(
    id                      INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    subject                 VARCHAR(200)    NOT NULL DEFAULT '',
    content                 TEXT            NOT NULL,
    sender_user_id          INTEGER         NOT NULL REFERENCES public.users (id),
    recipient_user_id       INTEGER         NOT NULL REFERENCES public.users (id),
    created_at              TIMESTAMPTZ     NOT NULL DEFAULT now(),
    read_at                 TIMESTAMPTZ     NULL
);

CREATE INDEX ix_messages_recipient ON public.messages (recipient_user_id, created_at DESC) INCLUDE (read_at);
CREATE INDEX ix_messages_sender ON public.messages (sender_user_id, created_at DESC);
