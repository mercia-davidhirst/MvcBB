-- Matches IRankRepository.Add(RankModel rank)

DROP PROCEDURE IF EXISTS dbo.usp_Rank_Insert;
GO

CREATE PROCEDURE dbo.usp_Rank_Insert
    @Title NVARCHAR(70),
    @MinPosts INT = 0,
    @ImagePath NVARCHAR(500) = NULL,
    @IsActive BIT = 1,
    @SortOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Ranks (Title, MinPosts, ImagePath, IsActive, SortOrder)
    OUTPUT inserted.Id, inserted.Title, inserted.MinPosts, inserted.ImagePath, inserted.IsActive, inserted.SortOrder
    VALUES (@Title, @MinPosts, @ImagePath, @IsActive, @SortOrder);
END;
GO
