/*
    Table:   dbo.Notifications
    Purpose: Backing store for MvcBB.Shared.Models.Notification.Notification /
             INotificationRepository - each member's notification centre
             (replies, @mentions, quotes, likes, private messages and
             moderation decisions). Added in v1.4.0.
    Notes:
      - Type mirrors NotificationType: 1 = Reply, 2 = Mention, 3 = Like,
        4 = Message, 5 = Quote, 6 = Moderation.
      - Only UserId has a foreign key (rows go with their member). ActorUserId,
        ThreadId, PostId and MessageId are kept for reference only: a
        notification about a thread that has since been deleted simply isn't
        shown (the API checks the thread is still there and visible), and
        SQL Server won't allow a second cascade path to dbo.Users anyway.
      - Excerpt is a short plain-text extract written at the time.
      - Read notifications older than Notifications:RetentionDays (API
        appsettings.json, default 90) are deleted daily.
*/

IF OBJECT_ID('dbo.Notifications') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        UserId          INT                NOT NULL,
        Type            TINYINT            NOT NULL,
        ActorUserId     INT                NULL,
        ThreadId        INT                NULL,
        PostId          INT                NULL,
        MessageId       INT                NULL,
        Excerpt         NVARCHAR(300)      NULL,
        IsRead          BIT                NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT CK_Notifications_Type CHECK (Type BETWEEN 1 AND 6)
    );

    CREATE NONCLUSTERED INDEX IX_Notifications_UserId_CreatedAt ON dbo.Notifications (UserId, CreatedAt DESC) INCLUDE (IsRead);
    -- Plain (not filtered) indexes on purpose: a filtered index would make every
    -- delete that cascades here (deleting a member) need QUOTED_IDENTIFIER ON,
    -- which older tools and scripts don't always set.
    CREATE NONCLUSTERED INDEX IX_Notifications_UserId_IsRead ON dbo.Notifications (UserId, IsRead) INCLUDE (ThreadId, MessageId);
    CREATE NONCLUSTERED INDEX IX_Notifications_PostId ON dbo.Notifications (PostId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here.
