/*
    Table:   dbo.PasswordPolicy
    Purpose: Backing store for MvcBB.Shared.Models.Settings.PasswordPolicy /
             IPasswordPolicyRepository. A single configuration row (Id is
             always 1, enforced by the CHECK constraint) - there's exactly
             one row, seeded once by Seed Data/18_PasswordPolicy.sql, and
             only ever UPDATEd (see usp_PasswordPolicy_Update), never
             inserted or deleted at runtime.
*/

DROP TABLE IF EXISTS dbo.PasswordPolicy;
GO

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
GO
