/*
    Table:   dbo.AdminEmailFailures
    Purpose: Members an admin email (dbo.AdminEmails) couldn't be sent to,
             with the reason, shown on the email's details page
             (MvcBB.Shared.Models.AdminEmail.AdminEmailFailure /
             IAdminEmailRepository.AddFailure/GetFailures).
    Notes:
      - UserId has no foreign key so the list survives an account being
        removed; Username and Email keep what was used when sending.
      - Reason is the mail server's or mail library's message, cut to 1000
        characters.
*/

IF OBJECT_ID('dbo.AdminEmailFailures') IS NULL
BEGIN
    CREATE TABLE dbo.AdminEmailFailures
    (
        Id            INT IDENTITY(1,1)  NOT NULL,
        AdminEmailId  INT                NOT NULL,
        UserId        INT                NOT NULL,
        Username      NVARCHAR(50)       NOT NULL,
        Email         NVARCHAR(256)      NOT NULL,
        Reason        NVARCHAR(1000)     NOT NULL,
        FailedAt      DATETIME2(3)       NOT NULL CONSTRAINT DF_AdminEmailFailures_FailedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AdminEmailFailures PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AdminEmailFailures_AdminEmail FOREIGN KEY (AdminEmailId) REFERENCES dbo.AdminEmails (Id)
    );

    CREATE NONCLUSTERED INDEX IX_AdminEmailFailures_AdminEmailId ON dbo.AdminEmailFailures (AdminEmailId);
END
GO
