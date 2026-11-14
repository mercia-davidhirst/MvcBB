/*
    Table:   dbo.PasswordPolicy
    Purpose: Backing store for MvcBB.Shared.Models.Settings.PasswordPolicy /
             IPasswordPolicyRepository. A single configuration row (Id is
             always 1, enforced by the CHECK constraint) - there's exactly
             one row, seeded once by Seed Data/18_PasswordPolicy.sql, and
             only ever UPDATEd (see usp_PasswordPolicy_Update), never
             inserted or deleted at runtime.
*/

IF OBJECT_ID('dbo.PasswordPolicy') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordPolicy
    (
        Id                      INT NOT NULL CONSTRAINT CK_PasswordPolicy_SingletonId CHECK (Id = 1),
        MinimumLength           INT NOT NULL,
        RequireUppercase        BIT NOT NULL,
        RequireLowercase        BIT NOT NULL,
        RequireDigit            BIT NOT NULL,
        RequireSpecialCharacter BIT NOT NULL,
        CONSTRAINT PK_PasswordPolicy PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PasswordPolicy') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.PasswordPolicy ADD NewColumn ...;
--   END
--   GO
