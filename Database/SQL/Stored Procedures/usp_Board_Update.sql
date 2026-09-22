-- Matches IBoardRepository.Update(Board board)

DROP PROCEDURE IF EXISTS dbo.usp_Board_Update;
GO

CREATE PROCEDURE dbo.usp_Board_Update
    @Id INT,
    @Name NVARCHAR(100),
    @Description NVARCHAR(500),
    @SortOrder INT,
    @IsActive BIT,
    @UpdatedAt DATETIME2(3) = NULL,
    @ThreadCount INT,
    @PostCount INT,
    @LastPostByUserId INT = NULL,
    @LastPostAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Boards
    SET Name = @Name,
        Description = @Description,
        SortOrder = @SortOrder,
        IsActive = @IsActive,
        UpdatedAt = ISNULL(@UpdatedAt, SYSUTCDATETIME()),
        ThreadCount = @ThreadCount,
        PostCount = @PostCount,
        LastPostByUserId = @LastPostByUserId,
        LastPostAt = @LastPostAt
    WHERE Id = @Id;
END;
GO
