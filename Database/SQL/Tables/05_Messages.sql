/*
    Table:   dbo.Messages
    Purpose: Backing store for MvcBB.Shared.Models.Message.Message /
             IMessageRepository.
    Notes:
      - IX_Messages_Recipient supports GetByRecipientUserId(unreadOnly) and
        GetUnreadCountByRecipientUserId, both filtered/sorted by recipient +
        read state + recency.
*/

IF OBJECT_ID('dbo.Messages') IS NULL
BEGIN
    CREATE TABLE dbo.Messages
    (
        Id                  INT IDENTITY(1,1)  NOT NULL,
        Subject             NVARCHAR(200)      NOT NULL CONSTRAINT DF_Messages_Subject DEFAULT (N''),
        Content             NVARCHAR(MAX)      NOT NULL,
        SenderUserId        INT                NOT NULL,
        RecipientUserId     INT                NOT NULL,
        CreatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_Messages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ReadAt               DATETIME2(3)      NULL,
        CONSTRAINT PK_Messages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Messages_Sender FOREIGN KEY (SenderUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Messages_Recipient FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users (Id)
    );

    CREATE NONCLUSTERED INDEX IX_Messages_Recipient ON dbo.Messages (RecipientUserId, CreatedAt DESC) INCLUDE (ReadAt);
    CREATE NONCLUSTERED INDEX IX_Messages_Sender ON dbo.Messages (SenderUserId, CreatedAt DESC);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Messages') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Messages ADD NewColumn ...;
--   END
--   GO
