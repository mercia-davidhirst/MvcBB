-- Matches IBBCodeManagementService.UpdateBBCodeTag(int id, BBCodeTagModel model)

DROP PROCEDURE IF EXISTS dbo.usp_BBCodeTag_Update;
GO

CREATE PROCEDURE dbo.usp_BBCodeTag_Update
    @Id INT,
    @Name NVARCHAR(100),
    @Pattern NVARCHAR(500),
    @Replacement NVARCHAR(1000),
    @Description NVARCHAR(500),
    @Example NVARCHAR(500),
    @IsActive BIT,
    @SortOrder INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BBCodeTags
    SET Name = @Name,
        Pattern = @Pattern,
        Replacement = @Replacement,
        Description = @Description,
        Example = @Example,
        IsActive = @IsActive,
        SortOrder = @SortOrder
    WHERE Id = @Id;
END;
GO
