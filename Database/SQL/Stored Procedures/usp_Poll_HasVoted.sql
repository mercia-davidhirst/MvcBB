-- Matches IPollRepository.HasVoted(int pollId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_Poll_HasVoted;
GO

CREATE PROCEDURE dbo.usp_Poll_HasVoted
    @PollId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM dbo.PollVotes WHERE PollId = @PollId AND UserId = @UserId
    ) THEN 1 ELSE 0 END AS BIT);
END;
GO
