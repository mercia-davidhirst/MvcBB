-- Matches IWordFilterRepository.GetAll() (ordered by SortOrder)

DROP PROCEDURE IF EXISTS dbo.usp_WordFilter_GetAll;
GO

CREATE PROCEDURE dbo.usp_WordFilter_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Word, Replacement, IsActive, SortOrder
    FROM dbo.WordFilters
    ORDER BY SortOrder;
END;
GO
