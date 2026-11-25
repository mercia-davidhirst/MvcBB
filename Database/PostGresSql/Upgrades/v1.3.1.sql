/*
    Upgrade: MvcBB v1.3.0 -> v1.3.1 (PostgreSQL)

    Schema changes only. Safe to run any number of times: every change is
    guarded, so a database that already has it (including a fresh v1.3.1
    install, whose Tables/ scripts create the new shape directly) is left as
    it is. No existing rows are changed or deleted.

    The new v1.3.1 table (thread_reads) is created by its Tables/ script;
    this only changes tables that existed before.

    install.ps1 runs this straight after Tables/ and before the functions,
    views and procedures that use the new columns, so re-running install.ps1
    is the normal way to upgrade. To run it on its own:
        psql -h <host> -U <user> -d <database> -v ON_ERROR_STOP=1 -f Upgrades/v1.3.1.sql

    1. users.auto_jump_to_unread - open discussions at the member's first
       unread post (a profile setting, on by default)
*/

-- 1. users.auto_jump_to_unread. Existing members get TRUE (on).
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS auto_jump_to_unread BOOLEAN NOT NULL DEFAULT TRUE;
