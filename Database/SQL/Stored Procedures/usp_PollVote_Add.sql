-- Matches IPollRepository.AddVote(int pollId, int pollOptionId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_PollVote_Add;
GO

CREATE PROCEDURE dbo.usp_PollVote_Add
    @PollId INT,
    @PollOptionId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PollVotes (PollId, PollOptionId, UserId)
    VALUES (@PollId, @PollOptionId, @UserId);
END;
GO
