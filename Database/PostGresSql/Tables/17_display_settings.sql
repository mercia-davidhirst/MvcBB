/*
    Table:   public.display_settings
    Purpose: Backing store for MvcBB.Shared.Models.Settings.DisplaySettings /
             IDisplaySettingsRepository. A single configuration row (id is
             always 1, enforced by the CHECK constraint) - there's exactly
             one row, seeded once by Seed Data/17_display_settings.sql, and
             only ever UPDATEd (see sp_display_settings_update), never
             inserted or deleted at runtime.
*/

CREATE TABLE IF NOT EXISTS public.display_settings
(
    id                   INTEGER         NOT NULL CHECK (id = 1),
    threads_per_page     INTEGER         NOT NULL,
    site_title           VARCHAR(100)    NOT NULL,
    welcome_message      VARCHAR(500)    NOT NULL,
    site_name            VARCHAR(50)     NOT NULL,
    logo_path            VARCHAR(500)    NULL,
    footer_disclaimer    VARCHAR(500)    NULL,
    registration_enabled BOOLEAN         NOT NULL,
    date_format          VARCHAR(20)     NOT NULL DEFAULT 'dd/MM/yyyy',
    time_format          VARCHAR(20)     NOT NULL DEFAULT 'HH:mm',
    -- Forum display time zone (IANA id, e.g. 'Europe/London'); blank = the
    -- web server's local time zone. Added in v1.1.0.
    time_zone_id         VARCHAR(100)    NOT NULL DEFAULT '',
    -- Host sending limits (Admin > Settings); 0 = use the API's
    -- AdminEmail:MaxPerHour / MaxPerDay. Added in v1.3.0.
    max_emails_per_hour  INTEGER         NOT NULL DEFAULT 0,
    max_emails_per_day   INTEGER         NOT NULL DEFAULT 0,
    -- Posts shown per page of a discussion. Added in v1.4.0.
    posts_per_page       INTEGER         NOT NULL DEFAULT 20,
    -- Home page "Who's online" card and how many minutes count as online.
    -- Added in v1.4.1.
    show_whos_online     BOOLEAN         NOT NULL DEFAULT TRUE,
    online_window_minutes INTEGER        NOT NULL DEFAULT 15,
    PRIMARY KEY (id)
);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS date_format VARCHAR(20) NOT NULL DEFAULT 'dd/MM/yyyy';
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS time_format VARCHAR(20) NOT NULL DEFAULT 'HH:mm';

-- v1.1.0 (time_zone_id) and v1.3.0 (max_emails_per_hour/max_emails_per_day)
-- are applied by Upgrades/v1.1.0.sql and v1.3.0.sql, which install.ps1 runs
-- straight after Tables/.
