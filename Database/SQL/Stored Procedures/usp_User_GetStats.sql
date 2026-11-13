-- Matches IUserRepository.GetStats(DateTime activeSince) - the homepage
-- statistics panel's total member / recently-active member counts, as a
-- single aggregate query instead of the caller loading every user to call
-- Count()/Count(predicate) in memory.

DROP PROCEDURE IF EXISTS dbo.usp_User_GetStats;
GO

CREATE PROCEDURE dbo.usp_User_GetStats
    @ActiveSince DATETIME2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(*) AS TotalMembers,
        ISNULL(SUM(CASE WHEN LastLoginAt >= @ActiveSince THEN 1 ELSE 0 END), 0) AS ActiveMembers
    FROM dbo.Users;
END;
GO
