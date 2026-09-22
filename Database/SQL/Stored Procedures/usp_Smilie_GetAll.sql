-- Matches IBBCodeManagementService.GetSmilies()

DROP PROCEDURE IF EXISTS dbo.usp_Smilie_GetAll;
GO

CREATE PROCEDURE dbo.usp_Smilie_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Code, Description, ImagePath, IsActive, SortOrder
    FROM dbo.Smilies
    ORDER BY SortOrder;
END;
GO
