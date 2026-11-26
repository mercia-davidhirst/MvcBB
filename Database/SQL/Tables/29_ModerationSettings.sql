/*
    Table:   dbo.ModerationSettings
    Purpose: Backing store for MvcBB.Shared.Models.Moderation.ModerationSettings /
             IModerationSettingsRepository. A single configuration row (Id is
             always 1, enforced by the CHECK constraint), seeded by
             Seed Data/19_ModerationSettings.sql and only ever UPDATEd
             afterwards. Added in v1.4.0.
    Notes:
      - HoldNewMemberPosts: hold posts from new members for approval (off by
        default, so upgrading changes nothing until an admin turns it on).
      - NewMemberPostThreshold: a member counts as new until this many of
        their posts are live.
*/

IF OBJECT_ID('dbo.ModerationSettings') IS NULL
BEGIN
    CREATE TABLE dbo.ModerationSettings
    (
        Id                      INT  NOT NULL CONSTRAINT CK_ModerationSettings_SingletonId CHECK (Id = 1),
        HoldNewMemberPosts      BIT  NOT NULL CONSTRAINT DF_ModerationSettings_HoldNewMemberPosts DEFAULT (0),
        NewMemberPostThreshold  INT  NOT NULL CONSTRAINT DF_ModerationSettings_NewMemberPostThreshold DEFAULT (1),
        CONSTRAINT PK_ModerationSettings PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ModerationSettings_Threshold CHECK (NewMemberPostThreshold BETWEEN 1 AND 100)
    );
END
GO

-- Schema changes for a database that already has an older version of this
-- table: none pending yet - add guarded ALTER TABLE statements here.
