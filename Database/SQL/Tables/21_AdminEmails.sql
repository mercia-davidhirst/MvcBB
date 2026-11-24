/*
    Table:   dbo.AdminEmails
    Purpose: Send log for emails admins send to members from the admin panel
             (MvcBB.Shared.Models.AdminEmail.AdminEmail / IAdminEmailRepository).
    Notes:
      - Audience mirrors AdminEmailAudience: 0 = all users, 1 = one role
        (Role), 2 = one user (UserId).
      - SenderMode mirrors AdminEmailSenderMode: 0 = the admin's address,
        1 = the site's no-reply address. FromAddress/FromName/ReplyToAddress
        record what was actually used.
      - Status mirrors AdminEmailStatus: 0 = Queued, 1 = Sending,
        2 = Completed, 3 = CompletedWithErrors, 4 = Interrupted.
      - SentByUserId has no foreign key so the log survives an account being
        removed; SentByUsername keeps the name as it was when sent.
*/

IF OBJECT_ID('dbo.AdminEmails') IS NULL
BEGIN
    CREATE TABLE dbo.AdminEmails
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        SentByUserId    INT                NOT NULL,
        SentByUsername  NVARCHAR(50)       NOT NULL,
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_AdminEmails_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CompletedAt     DATETIME2(3)       NULL,
        Subject         NVARCHAR(200)      NOT NULL,
        Body            NVARCHAR(MAX)      NOT NULL,
        Audience        TINYINT            NOT NULL,
        Role            TINYINT            NULL,
        UserId          INT                NULL,
        SenderMode      TINYINT            NOT NULL,
        FromAddress     NVARCHAR(256)      NOT NULL,
        FromName        NVARCHAR(256)      NULL,
        ReplyToAddress  NVARCHAR(256)      NULL,
        IsImportant     BIT                NOT NULL CONSTRAINT DF_AdminEmails_IsImportant DEFAULT (0),
        Status          TINYINT            NOT NULL CONSTRAINT DF_AdminEmails_Status DEFAULT (0),
        RecipientCount  INT                NOT NULL CONSTRAINT DF_AdminEmails_RecipientCount DEFAULT (0),
        SentCount       INT                NOT NULL CONSTRAINT DF_AdminEmails_SentCount DEFAULT (0),
        FailedCount     INT                NOT NULL CONSTRAINT DF_AdminEmails_FailedCount DEFAULT (0),
        CONSTRAINT PK_AdminEmails PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AdminEmails_Audience CHECK (Audience IN (0, 1, 2)),
        CONSTRAINT CK_AdminEmails_SenderMode CHECK (SenderMode IN (0, 1)),
        CONSTRAINT CK_AdminEmails_Status CHECK (Status IN (0, 1, 2, 3, 4))
    );

    CREATE NONCLUSTERED INDEX IX_AdminEmails_Status ON dbo.AdminEmails (Status);
END
GO
