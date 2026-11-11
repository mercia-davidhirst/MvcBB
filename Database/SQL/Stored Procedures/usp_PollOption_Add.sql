-- Matches IPollRepository.Add(Poll poll)'s per-option insert (SqlPollRepository.Add)

DROP PROCEDURE IF EXISTS dbo.usp_PollOption_Add;
GO

CREATE PROCEDURE dbo.usp_PollOption_Add
    @PollId INT,
    @Text NVARCHAR(200),
    @SortOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.PollOptions (PollId, Text, SortOrder)
    VALUES (@PollId, @Text, @SortOrder);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END;
GO
