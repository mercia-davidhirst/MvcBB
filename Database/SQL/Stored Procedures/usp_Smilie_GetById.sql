-- Matches IBBCodeManagementService.GetSmilie(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Smilie_GetById;
GO

CREATE PROCEDURE dbo.usp_Smilie_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Code, Description, ImagePath, IsActive, SortOrder
    FROM dbo.Smilies
    WHERE Id = @Id;
END;
GO
