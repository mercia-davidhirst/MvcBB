/*
    Table:   public.admin_email_deliveries
    Purpose: Which members an admin email (public.admin_emails) reached, and
             when (IAdminEmailRepository.AddDelivery/GetDeliveredUserIds).
    Notes:
      - Resuming an Interrupted email skips everyone listed here, so nobody
        gets it twice.
      - user_id has no foreign key, like admin_email_failures, so the record
        survives an account being removed.
*/

CREATE TABLE IF NOT EXISTS public.admin_email_deliveries
(
    admin_email_id      INTEGER         NOT NULL REFERENCES public.admin_emails (id),
    user_id             INTEGER         NOT NULL,
    sent_at             TIMESTAMPTZ     NOT NULL DEFAULT now(),
    PRIMARY KEY (admin_email_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_admin_email_deliveries_sent_at ON public.admin_email_deliveries (sent_at);
