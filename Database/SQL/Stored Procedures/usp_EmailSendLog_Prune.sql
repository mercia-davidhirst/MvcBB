-- Matches IEmailSendLogRepository.Prune(DateTime beforeUtc) - returns how
-- many rows were deleted.

DROP PROCEDURE IF EXISTS dbo.usp_EmailSendLog_Prune;
GO

CREATE PROCEDURE dbo.usp_EmailSendLog_Prune
    @Before DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.EmailSendLog WHERE SentAt < @Before;

    SELECT @@ROWCOUNT;
END;
GO
