/*
    Table:   public.admin_emails
    Purpose: Send log for emails admins send to members from the admin panel
             (MvcBB.Shared.Models.AdminEmail.AdminEmail / IAdminEmailRepository).
    Notes:
      - audience mirrors AdminEmailAudience: 0 = all users, 1 = one role
        (role), 2 = one user (user_id).
      - sender_mode mirrors AdminEmailSenderMode: 0 = the admin's address,
        1 = the site's no-reply address. from_address/from_name/
        reply_to_address record what was actually used.
      - status mirrors AdminEmailStatus: 0 = Queued, 1 = Sending,
        2 = Completed, 3 = CompletedWithErrors, 4 = Interrupted.
      - sent_by_user_id has no foreign key so the log survives an account
        being removed; sent_by_username keeps the name as it was when sent.
*/

CREATE TABLE IF NOT EXISTS public.admin_emails
(
    id                  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    sent_by_user_id     INTEGER         NOT NULL,
    sent_by_username    VARCHAR(50)     NOT NULL,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT now(),
    completed_at        TIMESTAMPTZ     NULL,
    subject             VARCHAR(200)    NOT NULL,
    body                TEXT            NOT NULL,
    audience            SMALLINT        NOT NULL,
    role                SMALLINT        NULL,
    user_id             INTEGER         NULL,
    sender_mode         SMALLINT        NOT NULL,
    from_address        VARCHAR(256)    NOT NULL,
    from_name           VARCHAR(256)    NULL,
    reply_to_address    VARCHAR(256)    NULL,
    is_important        BOOLEAN         NOT NULL DEFAULT FALSE,
    status              SMALLINT        NOT NULL DEFAULT 0,
    recipient_count     INTEGER         NOT NULL DEFAULT 0,
    sent_count          INTEGER         NOT NULL DEFAULT 0,
    failed_count        INTEGER         NOT NULL DEFAULT 0,
    CONSTRAINT ck_admin_emails_audience CHECK (audience IN (0, 1, 2)),
    CONSTRAINT ck_admin_emails_sender_mode CHECK (sender_mode IN (0, 1)),
    CONSTRAINT ck_admin_emails_status CHECK (status IN (0, 1, 2, 3, 4))
);

CREATE INDEX IF NOT EXISTS ix_admin_emails_status ON public.admin_emails (status);
