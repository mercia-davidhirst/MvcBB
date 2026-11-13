/*
    Table:   public.display_settings
    Purpose: Backing store for MvcBB.Shared.Models.Settings.DisplaySettings /
             IDisplaySettingsRepository. A single configuration row (id is
             always 1, enforced by the CHECK constraint) - there's exactly
             one row, seeded once by Seed Data/17_display_settings.sql, and
             only ever UPDATEd (see sp_display_settings_update), never
             inserted or deleted at runtime.
*/

DROP TABLE IF EXISTS public.display_settings CASCADE;

CREATE TABLE public.display_settings
(
    id                   INTEGER         NOT NULL CHECK (id = 1),
    threads_per_page     INTEGER         NOT NULL,
    site_title           VARCHAR(100)    NOT NULL,
    welcome_message      VARCHAR(500)    NOT NULL,
    site_name            VARCHAR(50)     NOT NULL,
    logo_path            VARCHAR(500)    NULL,
    footer_disclaimer    VARCHAR(500)    NULL,
    registration_enabled BOOLEAN         NOT NULL,
    PRIMARY KEY (id)
);
