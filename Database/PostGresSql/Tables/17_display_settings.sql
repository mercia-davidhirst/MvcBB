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
    PRIMARY KEY (id)
);

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS date_format VARCHAR(20) NOT NULL DEFAULT 'dd/MM/yyyy';
ALTER TABLE public.display_settings ADD COLUMN IF NOT EXISTS time_format VARCHAR(20) NOT NULL DEFAULT 'HH:mm';
