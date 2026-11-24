/*
    Table:   dbo.EmailSendLog
    Purpose: When the API sent each email and what kind it was
             (IEmailSendLogRepository), for the Sending activity chart and to
             refill the AdminEmail send-limit count after a restart.
    Notes:
      - Deliberately no addresses, subjects or user ids: just the time and
        the kind.
      - Kind mirrors EmailKind: 0 = Other, 1 = AdminEmail, 2 = PasswordReset,
        3 = Notification, 4 = Registration.
      - Rows older than AdminEmail:SendLogRetentionDays (default 30) are
        deleted daily by usp_EmailSendLog_Prune.
*/

IF OBJECT_ID('dbo.EmailSendLog') IS NULL
BEGIN
    CREATE TABLE dbo.EmailSendLog
    (
        Id      BIGINT IDENTITY(1,1)  NOT NULL,
        SentAt  DATETIME2(3)          NOT NULL CONSTRAINT DF_EmailSendLog_SentAt DEFAULT (SYSUTCDATETIME()),
        Kind    TINYINT               NOT NULL CONSTRAINT DF_EmailSendLog_Kind DEFAULT (0),
        CONSTRAINT PK_EmailSendLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_EmailSendLog_Kind CHECK (Kind IN (0, 1, 2, 3, 4))
    );

    CREATE NONCLUSTERED INDEX IX_EmailSendLog_SentAt ON dbo.EmailSendLog (SentAt) INCLUDE (Kind);
END
GO
