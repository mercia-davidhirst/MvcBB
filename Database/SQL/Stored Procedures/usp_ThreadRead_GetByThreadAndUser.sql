-- Matches IThreadReadRepository.GetByThreadAndUser(int threadId, string userId)

DROP PROCEDURE IF EXISTS dbo.usp_ThreadRead_GetByThreadAndUser;
GO

CREATE PROCEDURE dbo.usp_ThreadRead_GetByThreadAndUser
    @ThreadId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ThreadId, UserId, LastReadPostId, LastReadPostAt, UpdatedAt
    FROM dbo.ThreadReads
    WHERE ThreadId = @ThreadId AND UserId = @UserId;
END;
GO
