-- Matches IWordFilterRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_WordFilter_GetById;
GO

CREATE PROCEDURE dbo.usp_WordFilter_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Word, Replacement, IsActive, SortOrder
    FROM dbo.WordFilters
    WHERE Id = @Id;
END;
GO
