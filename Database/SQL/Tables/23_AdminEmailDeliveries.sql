/*
    Table:   dbo.AdminEmailDeliveries
    Purpose: Which members an admin email (dbo.AdminEmails) reached, and when
             (IAdminEmailRepository.AddDelivery/GetDeliveredUserIds).
    Notes:
      - Resuming an Interrupted email skips everyone listed here, so nobody
        gets it twice.
      - UserId has no foreign key, like AdminEmailFailures, so the record
        survives an account being removed.
*/

IF OBJECT_ID('dbo.AdminEmailDeliveries') IS NULL
BEGIN
    CREATE TABLE dbo.AdminEmailDeliveries
    (
        AdminEmailId  INT            NOT NULL,
        UserId        INT            NOT NULL,
        SentAt        DATETIME2(3)   NOT NULL CONSTRAINT DF_AdminEmailDeliveries_SentAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AdminEmailDeliveries PRIMARY KEY CLUSTERED (AdminEmailId, UserId),
        CONSTRAINT FK_AdminEmailDeliveries_AdminEmail FOREIGN KEY (AdminEmailId) REFERENCES dbo.AdminEmails (Id)
    );

    CREATE NONCLUSTERED INDEX IX_AdminEmailDeliveries_SentAt ON dbo.AdminEmailDeliveries (SentAt);
END
GO
