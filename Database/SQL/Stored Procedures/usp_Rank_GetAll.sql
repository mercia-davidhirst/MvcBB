-- Matches IRankRepository.GetAll() (ordered by SortOrder)

DROP PROCEDURE IF EXISTS dbo.usp_Rank_GetAll;
GO

CREATE PROCEDURE dbo.usp_Rank_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, MinPosts, ImagePath, IsActive, SortOrder
    FROM dbo.Ranks
    ORDER BY SortOrder;
END;
GO
