-- Matches IBBCodeManagementService.AddBBCodeTag(BBCodeTagModel model)

DROP PROCEDURE IF EXISTS dbo.usp_BBCodeTag_Insert;
GO

CREATE PROCEDURE dbo.usp_BBCodeTag_Insert
    @Name NVARCHAR(100),
    @Pattern NVARCHAR(500),
    @Replacement NVARCHAR(1000),
    @Description NVARCHAR(500) = N'',
    @Example NVARCHAR(500) = N'',
    @IsActive BIT = 1,
    @SortOrder INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.BBCodeTags (Name, Pattern, Replacement, Description, Example, IsActive, SortOrder)
    OUTPUT inserted.Id, inserted.Name, inserted.Pattern, inserted.Replacement, inserted.Description,
           inserted.Example, inserted.IsActive, inserted.SortOrder
    VALUES (@Name, @Pattern, @Replacement, @Description, @Example, @IsActive, @SortOrder);
END;
GO
