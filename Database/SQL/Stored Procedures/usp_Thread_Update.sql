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
    @ParticipantCount INT,
    @LastPostAt DATETIME2(3) = NULL,
    @LastPostByUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- TotalLikes is deliberately NOT part of this proc - it's owned
    -- exclusively by usp_PostLike_Add/Remove. The repository's Update() is
    -- called from several places (title/lock edits, view/post count bumps,
    -- participant count bumps) each working from a ForumThread snapshot that
    -- may be older than the latest like/unlike - including TotalLikes here
    -- would risk overwriting it with that stale snapshot.
    UPDATE dbo.Threads
    SET Title = @Title,
        BoardId = @BoardId,
        UpdatedAt = @UpdatedAt,
        IsSticky = @IsSticky,
        IsLocked = @IsLocked,
        ViewCount = @ViewCount,
        PostCount = @PostCount,
        ParticipantCount = @ParticipantCount,
        LastPostAt = @LastPostAt,
        LastPostByUserId = @LastPostByUserId
    WHERE Id = @Id;
END;
GO
