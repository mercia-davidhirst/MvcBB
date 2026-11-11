-- Matches IWordFilterRepository.Add(WordFilterModel filter)

DROP PROCEDURE IF EXISTS dbo.usp_WordFilter_Insert;
GO

CREATE PROCEDURE dbo.usp_WordFilter_Insert
    @Word NVARCHAR(100),
    @Replacement NVARCHAR(100) = N'',
    @IsActive BIT = 1,
    @SortOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.WordFilters (Word, Replacement, IsActive, SortOrder)
    OUTPUT inserted.Id, inserted.Word, inserted.Replacement, inserted.IsActive, inserted.SortOrder
    VALUES (@Word, @Replacement, @IsActive, @SortOrder);
END;
GO
