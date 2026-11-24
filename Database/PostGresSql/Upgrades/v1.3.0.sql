/*
    Upgrade: MvcBB v1.2.x -> v1.3.0 (PostgreSQL)

    Schema changes only. Safe to run any number of times: every change is
    guarded, so a database that already has it (including a fresh v1.3.0
    install, whose Tables/ scripts create the new shape directly) is left as
    it is. No existing rows are changed or deleted.

    The new v1.3.0 tables (admin_email_failures, admin_email_deliveries,
    email_send_log) are created by their Tables/ scripts; this only changes
    tables that existed before.

    install.ps1 runs this straight after Tables/ and before the functions,
    views and procedures that use the new columns, so re-running install.ps1
    is the normal way to upgrade. To run it on its own:
        psql -h <host> -U <user> -d <database> -v ON_ERROR_STOP=1 -f Upgrades/v1.3.0.sql

    1. display_settings.max_emails_per_hour / max_emails_per_day - host
       sending limits set in Admin > Settings (0 = use the API's appsettings.json)
    2. admin_emails.paused_until - when a sending email expects to carry on
       after waiting for those limits (the table was added in v1.2.0)
*/

-- 1. display_settings sending limits. Existing rows get 0 (no limit set here).
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS max_emails_per_hour INTEGER NOT NULL DEFAULT 0;
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS max_emails_per_day INTEGER NOT NULL DEFAULT 0;

-- 2. admin_emails.paused_until
ALTER TABLE IF EXISTS public.admin_emails ADD COLUMN IF NOT EXISTS paused_until TIMESTAMPTZ NULL;
