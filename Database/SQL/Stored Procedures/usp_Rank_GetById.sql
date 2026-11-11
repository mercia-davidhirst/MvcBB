-- Matches IRankRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Rank_GetById;
GO

CREATE PROCEDURE dbo.usp_Rank_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, MinPosts, ImagePath, IsActive, SortOrder
    FROM dbo.Ranks
    WHERE Id = @Id;
END;
GO
