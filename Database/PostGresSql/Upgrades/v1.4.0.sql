/*
    Upgrade: MvcBB v1.3.1 -> v1.4.0 (PostgreSQL)

    Schema changes, plus one new BBCode tag row. Safe to run any number of
    times: every change is guarded, so a database that already has it
    (including a fresh v1.4.0 install, whose Tables/ scripts create the new
    shape directly) is left as it is. No existing rows are changed or
    deleted.

    The new v1.4.0 tables are created by their own Tables/ scripts, which
    install.ps1 runs just before this one:
        Tables/26_notifications.sql        - the notification centre
        Tables/27_moderation_queue.sql     - posts held for approval
        Tables/28_moderation_log.sql       - the moderation log
        Tables/29_moderation_settings.sql  - when to hold new members' posts
                                             (seeded off by Seed Data/
                                             19_moderation_settings.sql)
    This script only changes what existed before.

    install.ps1 runs this straight after Tables/ and before the functions,
    views and procedures that use the new columns, so re-running install.ps1
    is the normal way to upgrade. To run it on its own:
        psql -h <host> -U <user> -d <database> -v ON_ERROR_STOP=1 -f Upgrades/v1.4.0.sql

    1. users.birth_day, birth_month, birth_year - a member's birthday, set on
       their profile. Day and month together, year optional. Existing members
       have none.
    2. bb_code_tags - adds the Spoiler tag to the reference list on
       Admin > BBCode, for a database whose list was seeded before v1.4.0
       (an empty table is left for Seed Data/07_bb_code_tags.sql, which now
       includes it). The tag itself works either way.
    3. display_settings.posts_per_page - how many posts a discussion shows
       per page (default 20; discussions are paged from v1.4.0).

    The user read functions return SETOF public.users, so they pick up the
    new columns without changes; sp_user_insert and sp_user_update are
    recreated by their Stored Procedures/ scripts with the new parameters.
*/

-- 1. users birthday columns.
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS birth_day SMALLINT NULL;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS birth_month SMALLINT NULL;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS birth_year SMALLINT NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_users_birthday') THEN
        ALTER TABLE public.users ADD CONSTRAINT ck_users_birthday CHECK ((birth_day IS NULL AND birth_month IS NULL AND birth_year IS NULL)
            OR (birth_day BETWEEN 1 AND 31 AND birth_month BETWEEN 1 AND 12 AND (birth_year IS NULL OR birth_year BETWEEN 1900 AND 9999)));
    END IF;
END $$;

-- 2. Spoiler in the BBCode tag list (existing, already-seeded lists only).
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM public.bb_code_tags)
       AND NOT EXISTS (SELECT 1 FROM public.bb_code_tags WHERE name = 'Spoiler') THEN
        INSERT INTO public.bb_code_tags (name, pattern, replacement, description, example, is_active, sort_order)
        SELECT 'Spoiler', '\[spoiler(=(.*?))?\](.*?)\[/spoiler\]', '<details class="bbcode-spoiler"><summary>$2</summary>$3</details>',
               'Hides text until the reader clicks it, with an optional title', '[spoiler=Ending]hidden text[/spoiler]', TRUE,
               COALESCE(MAX(sort_order), 0) + 1
        FROM public.bb_code_tags;
    END IF;
END $$;

-- 3. display_settings.posts_per_page - posts per page of a discussion (default 20).
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS posts_per_page INTEGER NOT NULL DEFAULT 20;
