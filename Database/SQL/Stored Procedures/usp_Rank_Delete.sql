-- Matches IRankRepository.Remove(RankModel rank)

DROP PROCEDURE IF EXISTS dbo.usp_Rank_Delete;
GO

CREATE PROCEDURE dbo.usp_Rank_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Ranks
    WHERE Id = @Id;
END;
GO
