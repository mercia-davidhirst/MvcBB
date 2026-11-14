/*
    Table:   dbo.ThreadSubscriptions
    Purpose: Backing store for
             MvcBB.Shared.Models.ThreadSubscription.ThreadSubscriptionModel /
             IThreadSubscriptionRepository - one row per user "watching" a
             thread, created only when they explicitly subscribe (no
             auto-subscribe on reply/create).
    Notes:
      - NotifyViaMessage/NotifyViaEmail are the delivery channels the user
        picked; at least one is normally true, but neither is enforced NOT
        NULL=false-both since a subscription with both off is harmless (it
        just never actually notifies - the record still marks "watching").
      - One subscription per (ThreadId, UserId) - re-subscribing updates the
        existing row's notify flags instead of creating a duplicate.
*/

IF OBJECT_ID('dbo.ThreadSubscriptions') IS NULL
BEGIN
    CREATE TABLE dbo.ThreadSubscriptions
    (
        Id                  INT IDENTITY(1,1)  NOT NULL,
        ThreadId            INT                NOT NULL,
        UserId              INT                NOT NULL,
        NotifyViaMessage    BIT                NOT NULL CONSTRAINT DF_ThreadSubscriptions_NotifyViaMessage DEFAULT (1),
        NotifyViaEmail      BIT                NOT NULL CONSTRAINT DF_ThreadSubscriptions_NotifyViaEmail DEFAULT (0),
        CreatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_ThreadSubscriptions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ThreadSubscriptions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_ThreadSubscriptions_Thread_User UNIQUE (ThreadId, UserId),
        CONSTRAINT FK_ThreadSubscriptions_Thread FOREIGN KEY (ThreadId) REFERENCES dbo.Threads (Id),
        CONSTRAINT FK_ThreadSubscriptions_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
    );

    CREATE NONCLUSTERED INDEX IX_ThreadSubscriptions_ThreadId ON dbo.ThreadSubscriptions (ThreadId);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ThreadSubscriptions') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.ThreadSubscriptions ADD NewColumn ...;
--   END
--   GO
