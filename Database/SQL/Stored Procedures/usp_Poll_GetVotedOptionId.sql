-- Matches IPollRepository.GetVotedOptionId(int pollId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_Poll_GetVotedOptionId;
GO

CREATE PROCEDURE dbo.usp_Poll_GetVotedOptionId
    @PollId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT PollOptionId
    FROM dbo.PollVotes
    WHERE PollId = @PollId AND UserId = @UserId;
END;
GO
