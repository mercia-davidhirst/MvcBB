/*
    Table:   public.admin_email_opt_outs
    Purpose: Members who don't want emails sent by admins from the admin
             panel (IAdminEmailRepository.IsOptedOut/SetOptedOut). A row
             means "opted out"; no row means the default, opted in.
    Notes:
      - Kept out of public.users on purpose: every user query and the
        whole-row sp_user_update would otherwise have to carry the flag, and
        any that didn't would quietly opt the member back in.
      - Important service alerts (admin_emails.is_important) are still sent
        to members listed here.
*/

CREATE TABLE IF NOT EXISTS public.admin_email_opt_outs
(
    user_id         INTEGER         PRIMARY KEY REFERENCES public.users (id),
    opted_out_at    TIMESTAMPTZ     NOT NULL DEFAULT now()
);
