-- Matches IPollRepository.Add(Poll poll) - inserts the poll row only; options
-- are added one at a time afterwards via usp_PollOption_Add (SqlPollRepository.Add).

DROP PROCEDURE IF EXISTS dbo.usp_Poll_Create;
GO

CREATE PROCEDURE dbo.usp_Poll_Create
    @ThreadId INT,
    @Question NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Polls (ThreadId, Question)
    VALUES (@ThreadId, @Question);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END;
GO
