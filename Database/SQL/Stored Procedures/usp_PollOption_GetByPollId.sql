-- Matches IPollRepository.GetByThreadId/GetById's option lookup (SqlPollRepository.WithOptions)

DROP PROCEDURE IF EXISTS dbo.usp_PollOption_GetByPollId;
GO

CREATE PROCEDURE dbo.usp_PollOption_GetByPollId
    @PollId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, PollId, Text, SortOrder
    FROM dbo.PollOptions
    WHERE PollId = @PollId
    ORDER BY SortOrder;
END;
GO
