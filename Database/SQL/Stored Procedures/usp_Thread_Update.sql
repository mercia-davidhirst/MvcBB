-- Matches IForumThreadRepository.Update(ForumThread thread)

DROP PROCEDURE IF EXISTS dbo.usp_Thread_Update;
GO

CREATE PROCEDURE dbo.usp_Thread_Update
    @Id INT,
    @Title NVARCHAR(200),
    @BoardId INT,
    @UpdatedAt DATETIME2(3) = NULL,
    @IsSticky BIT,
    @IsLocked BIT,
    @ViewCount INT,
    @PostCount INT,
    @LastPostAt DATETIME2(3) = NULL,
    @LastPostByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Threads
    SET Title = @Title,
        BoardId = @BoardId,
        UpdatedAt = @UpdatedAt,
        IsSticky = @IsSticky,
        IsLocked = @IsLocked,
        ViewCount = @ViewCount,
        PostCount = @PostCount,
        LastPostAt = @LastPostAt,
        LastPostByUserId = @LastPostByUserId
    WHERE Id = @Id;
END;
GO
