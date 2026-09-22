-- Matches IBBCodeManagementService.AddSmilie(SmilieModel model)

DROP PROCEDURE IF EXISTS dbo.usp_Smilie_Insert;
GO

CREATE PROCEDURE dbo.usp_Smilie_Insert
    @Code NVARCHAR(20),
    @Description NVARCHAR(200) = N'',
    @ImagePath NVARCHAR(500) = N'',
    @IsActive BIT = 1,
    @SortOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Smilies (Code, Description, ImagePath, IsActive, SortOrder)
    OUTPUT inserted.Id, inserted.Code, inserted.Description, inserted.ImagePath, inserted.IsActive, inserted.SortOrder
    VALUES (@Code, @Description, @ImagePath, @IsActive, @SortOrder);
END;
GO
