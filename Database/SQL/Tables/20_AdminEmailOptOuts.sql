/*
    Table:   dbo.AdminEmailOptOuts
    Purpose: Members who don't want emails sent by admins from the admin
             panel (IAdminEmailRepository.IsOptedOut/SetOptedOut). A row
             means "opted out"; no row means the default, opted in.
    Notes:
      - Kept out of dbo.Users on purpose: every user query and the whole-row
        usp_User_Update would otherwise have to carry the flag, and any that
        didn't would quietly opt the member back in.
      - Important service alerts (AdminEmails.IsImportant) are still sent to
        members listed here.
*/

IF OBJECT_ID('dbo.AdminEmailOptOuts') IS NULL
BEGIN
    CREATE TABLE dbo.AdminEmailOptOuts
    (
        UserId      INT            NOT NULL,
        OptedOutAt  DATETIME2(3)   NOT NULL CONSTRAINT DF_AdminEmailOptOuts_OptedOutAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AdminEmailOptOuts PRIMARY KEY CLUSTERED (UserId),
        CONSTRAINT FK_AdminEmailOptOuts_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
    );
END
GO
