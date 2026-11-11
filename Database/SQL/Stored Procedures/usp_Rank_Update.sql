-- Matches IRankRepository.Update(RankModel rank)

DROP PROCEDURE IF EXISTS dbo.usp_Rank_Update;
GO

CREATE PROCEDURE dbo.usp_Rank_Update
    @Id INT,
    @Title NVARCHAR(70),
    @MinPosts INT,
    @ImagePath NVARCHAR(500) = NULL,
    @IsActive BIT,
    @SortOrder INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Ranks
    SET Title = @Title,
        MinPosts = @MinPosts,
        ImagePath = @ImagePath,
        IsActive = @IsActive,
        SortOrder = @SortOrder
    WHERE Id = @Id;
END;
GO
