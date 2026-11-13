-- Matches IForumThreadRepository.Add(ForumThread thread)

DROP PROCEDURE IF EXISTS dbo.usp_Thread_Insert;
GO

CREATE PROCEDURE dbo.usp_Thread_Insert
    @Title NVARCHAR(200),
    @BoardId INT,
    @CreatedByUserId INT,
    @CreatedAt DATETIME2(3) = NULL,
    @IsSticky BIT = 0,
    @IsLocked BIT = 0,
    @ViewCount INT = 0,
    @PostCount INT = 0,
    @LastPostAt DATETIME2(3) = NULL,
    @LastPostByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- OUTPUT can only reference inserted./deleted. columns, not a join to
    -- Boards/Users for BoardName/CreatedByUsername/etc. - return just the new
    -- Id and let the repository re-fetch the full view-backed row via
    -- usp_Thread_GetById (same two-step pattern usp_Post_Insert already uses).
    INSERT INTO dbo.Threads (Title, BoardId, CreatedByUserId, CreatedAt, IsSticky, IsLocked, ViewCount, PostCount, LastPostAt, LastPostByUserId)
    OUTPUT inserted.Id
    VALUES (@Title, @BoardId, @CreatedByUserId, ISNULL(@CreatedAt, SYSUTCDATETIME()), @IsSticky, @IsLocked, @ViewCount, @PostCount, @LastPostAt, @LastPostByUserId);
END;
GO
