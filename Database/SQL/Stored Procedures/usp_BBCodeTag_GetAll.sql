-- Matches IBBCodeManagementService.GetBBCodeTags()

DROP PROCEDURE IF EXISTS dbo.usp_BBCodeTag_GetAll;
GO

CREATE PROCEDURE dbo.usp_BBCodeTag_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Pattern, Replacement, Description, Example, IsActive, SortOrder
    FROM dbo.BBCodeTags
    ORDER BY SortOrder;
END;
GO
