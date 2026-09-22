-- Matches IPostRepository.Update(Post post)

DROP PROCEDURE IF EXISTS dbo.usp_Post_Update;
GO

CREATE PROCEDURE dbo.usp_Post_Update
    @Id INT,
    @Content NVARCHAR(MAX),
    @IsEdited BIT,
    @EditReason NVARCHAR(500) = NULL,
    @UpdatedAt DATETIME2(3) = NULL,
    @UpdatedByUserId INT = NULL,
    @IsDeleted BIT,
    @DeletedByUserId INT = NULL,
    @DeletedAt DATETIME2(3) = NULL,
    @DeleteReason NVARCHAR(500) = NULL,
    @QuotedPostId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Posts
    SET Content = @Content,
        IsEdited = @IsEdited,
        EditReason = @EditReason,
        UpdatedAt = @UpdatedAt,
        UpdatedByUserId = @UpdatedByUserId,
        IsDeleted = @IsDeleted,
        DeletedByUserId = @DeletedByUserId,
        DeletedAt = @DeletedAt,
        DeleteReason = @DeleteReason,
        QuotedPostId = @QuotedPostId
    WHERE Id = @Id;
END;
GO
