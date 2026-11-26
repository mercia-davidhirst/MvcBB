-- Matches IModerationQueueRepository.Count() (@UserId null) and
-- CountByUser(int userId).

DROP PROCEDURE IF EXISTS dbo.usp_ModerationQueue_Count;
GO

CREATE PROCEDURE dbo.usp_ModerationQueue_Count
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.ModerationQueue WHERE @UserId IS NULL OR UserId = @UserId;
END;
GO
