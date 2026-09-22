/*
    Table:   dbo.Messages
    Purpose: Backing store for MvcBB.Shared.Models.Message.Message /
             IMessageRepository.
    Notes:
      - IX_Messages_Recipient supports GetByRecipientUserId(unreadOnly) and
        GetUnreadCountByRecipientUserId, both filtered/sorted by recipient +
        read state + recency.
*/

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Messages');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Messages;
GO

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
GO

CREATE NONCLUSTERED INDEX IX_Messages_Recipient ON dbo.Messages (RecipientUserId, CreatedAt DESC) INCLUDE (ReadAt);
GO

CREATE NONCLUSTERED INDEX IX_Messages_Sender ON dbo.Messages (SenderUserId, CreatedAt DESC);
GO
