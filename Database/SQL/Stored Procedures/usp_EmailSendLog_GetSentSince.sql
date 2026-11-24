-- Matches IEmailSendLogRepository.GetSentTimesSince(DateTime sinceUtc)

DROP PROCEDURE IF EXISTS dbo.usp_EmailSendLog_GetSentSince;
GO

CREATE PROCEDURE dbo.usp_EmailSendLog_GetSentSince
    @Since DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SentAt
    FROM dbo.EmailSendLog
    WHERE SentAt > @Since
    ORDER BY SentAt;
END;
GO
