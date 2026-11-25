-- Matches IThreadReadRepository.MarkRead(int threadId, string userId, int postId, DateTime postCreatedAt)
-- Creates the row the first time, then only moves it forward: a post created
-- at or before the saved position leaves the row as it is.

DROP PROCEDURE IF EXISTS dbo.usp_ThreadRead_MarkRead;
GO

CREATE PROCEDURE dbo.usp_ThreadRead_MarkRead
    @ThreadId INT,
    @UserId INT,
    @LastReadPostId INT,
    @LastReadPostAt DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ThreadReads
    SET LastReadPostId = @LastReadPostId,
        LastReadPostAt = @LastReadPostAt,
        UpdatedAt = SYSUTCDATETIME()
    WHERE ThreadId = @ThreadId
      AND UserId = @UserId
      AND LastReadPostAt < @LastReadPostAt;

    IF @@ROWCOUNT = 0
       AND NOT EXISTS (SELECT 1 FROM dbo.ThreadReads WHERE ThreadId = @ThreadId AND UserId = @UserId)
    BEGIN
        BEGIN TRY
            INSERT INTO dbo.ThreadReads (ThreadId, UserId, LastReadPostId, LastReadPostAt)
            VALUES (@ThreadId, @UserId, @LastReadPostId, @LastReadPostAt);
        END TRY
        BEGIN CATCH
            -- Two tabs saving at once: the other one created the row first.
            -- Move it forward instead (still only if this post is newer).
            IF ERROR_NUMBER() NOT IN (2601, 2627) THROW;

            UPDATE dbo.ThreadReads
            SET LastReadPostId = @LastReadPostId,
                LastReadPostAt = @LastReadPostAt,
                UpdatedAt = SYSUTCDATETIME()
            WHERE ThreadId = @ThreadId
              AND UserId = @UserId
              AND LastReadPostAt < @LastReadPostAt;
        END CATCH
    END
END;
GO
