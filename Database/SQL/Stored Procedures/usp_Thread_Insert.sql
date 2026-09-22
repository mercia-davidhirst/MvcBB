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

    INSERT INTO dbo.Threads (Title, BoardId, CreatedByUserId, CreatedAt, IsSticky, IsLocked, ViewCount, PostCount, LastPostAt, LastPostByUserId)
    OUTPUT inserted.Id, inserted.Title, inserted.BoardId, inserted.CreatedByUserId, inserted.CreatedAt,
           inserted.UpdatedAt, inserted.IsSticky, inserted.IsLocked, inserted.ViewCount, inserted.PostCount,
           inserted.LastPostAt, inserted.LastPostByUserId
    VALUES (@Title, @BoardId, @CreatedByUserId, ISNULL(@CreatedAt, SYSUTCDATETIME()), @IsSticky, @IsLocked, @ViewCount, @PostCount, @LastPostAt, @LastPostByUserId);
END;
GO
