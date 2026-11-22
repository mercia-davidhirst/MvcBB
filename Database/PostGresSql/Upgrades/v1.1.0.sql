/*
    Upgrade: MvcBB v1.0.0 -> v1.1.0 (PostgreSQL)

    Schema changes only. Safe to run any number of times: every change is
    guarded, so a database that already has it (including a fresh v1.1.0
    install, whose Tables/ scripts create the new shape directly) is left as
    it is. No existing rows are changed or deleted.

    install.ps1 runs this straight after Tables/ and before the functions,
    views and procedures that use the new columns, so re-running install.ps1
    is the normal way to upgrade. To run it on its own:
        psql -h <host> -U <user> -d <database> -v ON_ERROR_STOP=1 -f Upgrades/v1.1.0.sql

    1. reports.page_url              - page attached to a "Report an issue" report
    2. reports ck_reports_type       - allows type 3 (User) and 4 (SiteIssue)
    3. display_settings.time_zone_id - forum display time zone ('' = server local time)
*/

-- 1. reports.page_url
ALTER TABLE public.reports ADD COLUMN IF NOT EXISTS page_url VARCHAR(500) NULL;

-- 2. ck_reports_type: v1.0.0 only allowed 0-2, which also rejected User (3)
-- reports. Recreated only when the current definition doesn't allow 4.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.reports'::regclass
          AND conname = 'ck_reports_type'
          AND pg_get_constraintdef(oid) NOT LIKE '%4%'
    ) THEN
        ALTER TABLE public.reports DROP CONSTRAINT ck_reports_type;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.reports'::regclass
          AND conname = 'ck_reports_type'
    ) THEN
        ALTER TABLE public.reports ADD CONSTRAINT ck_reports_type CHECK (type IN (0, 1, 2, 3, 4));
    END IF;
END;
$$;

-- 3. display_settings.time_zone_id. Existing rows get '' (server local
-- time), which is how dates were shown before v1.1.0.
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS time_zone_id VARCHAR(100) NOT NULL DEFAULT '';
