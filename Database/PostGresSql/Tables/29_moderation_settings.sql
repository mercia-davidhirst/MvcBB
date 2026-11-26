/*
    Table:   public.moderation_settings
    Purpose: Backing store for MvcBB.Shared.Models.Moderation.ModerationSettings /
             IModerationSettingsRepository. A single configuration row (id is
             always 1, enforced by the CHECK constraint), seeded by
             Seed Data/19_moderation_settings.sql and only ever UPDATEd
             afterwards. Added in v1.4.0.
    Notes:
      - hold_new_member_posts: hold posts from new members for approval (off
        by default, so upgrading changes nothing until an admin turns it on).
      - new_member_post_threshold: a member counts as new until this many of
        their posts are live.
*/

CREATE TABLE IF NOT EXISTS public.moderation_settings
(
    id                          INTEGER NOT NULL CHECK (id = 1),
    hold_new_member_posts       BOOLEAN NOT NULL DEFAULT FALSE,
    new_member_post_threshold   INTEGER NOT NULL DEFAULT 1 CHECK (new_member_post_threshold BETWEEN 1 AND 100),
    PRIMARY KEY (id)
);

-- Schema changes for a database that already has an older version of this
-- table: none pending yet - add guarded ALTER TABLE statements here.
