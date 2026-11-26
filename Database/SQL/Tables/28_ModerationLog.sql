/*
    Table:   dbo.ModerationLog
    Purpose: Backing store for MvcBB.Shared.Models.Moderation.ModerationLogEntry /
             IModerationLogRepository - every moderator action (approving or
             turning down held posts, deleting and editing posts, locking,
             pinning and deleting threads, locking members, changing roles,
             updating reports). Added in v1.4.0.
    Notes:
      - Written by the API as each action happens; the site never edits or
        deletes rows.
      - ActorUserId and TargetUserId have no foreign keys, so the log keeps
        its history even if an account is later removed. Summary is written
        at the time (e.g. the thread's title then), so it still reads
        correctly after the thread is renamed or deleted.
*/

IF OBJECT_ID('dbo.ModerationLog') IS NULL
BEGIN
    CREATE TABLE dbo.ModerationLog
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        ActorUserId     INT                NOT NULL,
        Action          NVARCHAR(50)       NOT NULL,
        TargetType      NVARCHAR(20)       NOT NULL,
        TargetId        INT                NULL,
        TargetUserId    INT                NULL,
        Summary         NVARCHAR(500)      NOT NULL,
        Reason          NVARCHAR(500)      NULL,
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_ModerationLog_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ModerationLog PRIMARY KEY CLUSTERED (Id)
    );

    CREATE NONCLUSTERED INDEX IX_ModerationLog_CreatedAt ON dbo.ModerationLog (CreatedAt DESC);
END
GO

-- Schema changes for a database that already has an older version of this
-- table: none pending yet - add guarded ALTER TABLE statements here.
