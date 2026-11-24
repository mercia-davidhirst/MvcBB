-- Matches IEmailSendLogRepository.GetCounts(DateTime fromUtc, DateTime toUtc, int bucketMinutes)
-- Counts per kind in slots of @BucketMinutes starting at @From. Empty slots
-- aren't returned.

DROP PROCEDURE IF EXISTS dbo.usp_EmailSendLog_GetCounts;
GO

CREATE PROCEDURE dbo.usp_EmailSendLog_GetCounts
    @From DATETIME2(3),
    @To DATETIME2(3),
    @BucketMinutes INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DATEADD(MINUTE, (DATEDIFF(MINUTE, @From, SentAt) / @BucketMinutes) * @BucketMinutes, @From) AS BucketStart,
           Kind,
           COUNT(*) AS [Count]
    FROM dbo.EmailSendLog
    WHERE SentAt >= @From AND SentAt < @To
    GROUP BY DATEADD(MINUTE, (DATEDIFF(MINUTE, @From, SentAt) / @BucketMinutes) * @BucketMinutes, @From), Kind
    ORDER BY BucketStart, Kind;
END;
GO
