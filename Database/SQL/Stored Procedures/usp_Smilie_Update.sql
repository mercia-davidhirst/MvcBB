-- Matches IBBCodeManagementService.UpdateSmilie(int id, SmilieModel model)

DROP PROCEDURE IF EXISTS dbo.usp_Smilie_Update;
GO

CREATE PROCEDURE dbo.usp_Smilie_Update
    @Id INT,
    @Code NVARCHAR(20),
    @Description NVARCHAR(200),
    @ImagePath NVARCHAR(500),
    @IsActive BIT,
    @SortOrder INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Smilies
    SET Code = @Code,
        Description = @Description,
        ImagePath = @ImagePath,
        IsActive = @IsActive,
        SortOrder = @SortOrder
    WHERE Id = @Id;
END;
GO
