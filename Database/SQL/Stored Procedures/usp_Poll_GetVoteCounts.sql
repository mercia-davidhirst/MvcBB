-- Matches IPollRepository.GetVoteCounts(int pollId) - options with zero votes
-- simply aren't in the result; the caller (ThreadsController.MapToPollResponse)
-- already treats a missing key as 0.

DROP PROCEDURE IF EXISTS dbo.usp_Poll_GetVoteCounts;
GO

CREATE PROCEDURE dbo.usp_Poll_GetVoteCounts
    @PollId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT PollOptionId, COUNT(*) AS VoteCount
    FROM dbo.PollVotes
    WHERE PollId = @PollId
    GROUP BY PollOptionId;
END;
GO
