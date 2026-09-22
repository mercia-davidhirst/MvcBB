-- Matches IBBCodeManagementService.DeleteSmilie(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Smilie_Delete;
GO

CREATE PROCEDURE dbo.usp_Smilie_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Smilies
    WHERE Id = @Id;
END;
GO
