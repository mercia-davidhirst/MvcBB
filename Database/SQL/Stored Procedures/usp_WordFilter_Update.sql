-- Matches IWordFilterRepository.Update(WordFilterModel filter)

DROP PROCEDURE IF EXISTS dbo.usp_WordFilter_Update;
GO

CREATE PROCEDURE dbo.usp_WordFilter_Update
    @Id INT,
    @Word NVARCHAR(100),
    @Replacement NVARCHAR(100),
    @IsActive BIT,
    @SortOrder INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.WordFilters
    SET Word = @Word,
        Replacement = @Replacement,
        IsActive = @IsActive,
        SortOrder = @SortOrder
    WHERE Id = @Id;
END;
GO
