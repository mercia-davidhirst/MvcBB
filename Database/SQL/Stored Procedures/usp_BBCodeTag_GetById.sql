-- Matches IBBCodeManagementService.GetBBCodeTag(int id)

DROP PROCEDURE IF EXISTS dbo.usp_BBCodeTag_GetById;
GO

CREATE PROCEDURE dbo.usp_BBCodeTag_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Pattern, Replacement, Description, Example, IsActive, SortOrder
    FROM dbo.BBCodeTags
    WHERE Id = @Id;
END;
GO
