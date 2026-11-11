-- Matches IWordFilterRepository.Remove(WordFilterModel filter)

DROP PROCEDURE IF EXISTS dbo.usp_WordFilter_Delete;
GO

CREATE PROCEDURE dbo.usp_WordFilter_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.WordFilters
    WHERE Id = @Id;
END;
GO
