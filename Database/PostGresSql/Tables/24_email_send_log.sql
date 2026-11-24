/*
    Table:   public.email_send_log
    Purpose: When the API sent each email and what kind it was
             (IEmailSendLogRepository), for the Sending activity chart and to
             refill the AdminEmail send-limit count after a restart.
    Notes:
      - Deliberately no addresses, subjects or user ids: just the time and
        the kind.
      - kind mirrors EmailKind: 0 = Other, 1 = AdminEmail, 2 = PasswordReset,
        3 = Notification, 4 = Registration.
      - Rows older than AdminEmail:SendLogRetentionDays (default 30) are
        deleted daily by fn_email_send_log_prune.
*/

CREATE TABLE IF NOT EXISTS public.email_send_log
(
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sent_at     TIMESTAMPTZ     NOT NULL DEFAULT now(),
    kind        SMALLINT        NOT NULL DEFAULT 0,
    CONSTRAINT ck_email_send_log_kind CHECK (kind IN (0, 1, 2, 3, 4))
);

CREATE INDEX IF NOT EXISTS ix_email_send_log_sent_at ON public.email_send_log (sent_at);
