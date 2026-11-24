/*
    Table:   public.admin_email_failures
    Purpose: Members an admin email (public.admin_emails) couldn't be sent
             to, with the reason, shown on the email's details page
             (MvcBB.Shared.Models.AdminEmail.AdminEmailFailure /
             IAdminEmailRepository.AddFailure/GetFailures).
    Notes:
      - user_id has no foreign key so the list survives an account being
        removed; username and email keep what was used when sending.
      - reason is the mail server's or mail library's message, cut to 1000
        characters.
*/

CREATE TABLE IF NOT EXISTS public.admin_email_failures
(
    id                  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_email_id      INTEGER         NOT NULL REFERENCES public.admin_emails (id),
    user_id             INTEGER         NOT NULL,
    username            VARCHAR(50)     NOT NULL,
    email               VARCHAR(256)    NOT NULL,
    reason              VARCHAR(1000)   NOT NULL,
    failed_at           TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_admin_email_failures_admin_email_id ON public.admin_email_failures (admin_email_id);
