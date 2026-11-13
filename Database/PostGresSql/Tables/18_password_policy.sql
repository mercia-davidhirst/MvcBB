/*
    Table:   public.password_policy
    Purpose: Backing store for MvcBB.Shared.Models.Settings.PasswordPolicy /
             IPasswordPolicyRepository. A single configuration row (id is
             always 1, enforced by the CHECK constraint) - there's exactly
             one row, seeded once by Seed Data/18_password_policy.sql, and
             only ever UPDATEd (see sp_password_policy_update), never
             inserted or deleted at runtime.
*/

DROP TABLE IF EXISTS public.password_policy CASCADE;

CREATE TABLE public.password_policy
(
    id                        INTEGER NOT NULL CHECK (id = 1),
    minimum_length            INTEGER NOT NULL,
    require_uppercase         BOOLEAN NOT NULL,
    require_lowercase         BOOLEAN NOT NULL,
    require_digit             BOOLEAN NOT NULL,
    require_special_character BOOLEAN NOT NULL,
    PRIMARY KEY (id)
);
