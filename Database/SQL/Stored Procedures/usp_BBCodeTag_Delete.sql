-- Matches IBBCodeManagementService.DeleteBBCodeTag(int id)

DROP PROCEDURE IF EXISTS dbo.usp_BBCodeTag_Delete;
GO

CREATE PROCEDURE dbo.usp_BBCodeTag_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.BBCodeTags
    WHERE Id = @Id;
END;
GO
