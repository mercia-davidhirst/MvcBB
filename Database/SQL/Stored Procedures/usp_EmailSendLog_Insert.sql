-- Matches IEmailSendLogRepository.Add(DateTime sentAtUtc, EmailKind kind)

DROP PROCEDURE IF EXISTS dbo.usp_EmailSendLog_Insert;
GO

CREATE PROCEDURE dbo.usp_EmailSendLog_Insert
    @SentAt DATETIME2(3) = NULL,
    @Kind TINYINT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.EmailSendLog (SentAt, Kind)
    VALUES (ISNULL(@SentAt, SYSUTCDATETIME()), @Kind);
END;
GO
