-- Matches IBoardRepository.Add(Board board)

DROP PROCEDURE IF EXISTS dbo.usp_Board_Insert;
GO

CREATE PROCEDURE dbo.usp_Board_Insert
    @Name NVARCHAR(100),
    @Description NVARCHAR(500) = N'',
    @SortOrder INT = 0,
    @IsActive BIT = 1,
    @CreatedAt DATETIME2(3) = NULL,
    @ThreadCount INT = 0,
    @PostCount INT = 0,
    @LastPostByUserId INT = NULL,
    @LastPostAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Boards (Name, Description, SortOrder, IsActive, CreatedAt, ThreadCount, PostCount, LastPostByUserId, LastPostAt)
    OUTPUT inserted.Id, inserted.Name, inserted.Description, inserted.SortOrder, inserted.IsActive,
           inserted.CreatedAt, inserted.UpdatedAt, inserted.ThreadCount, inserted.PostCount,
           inserted.LastPostByUserId, inserted.LastPostAt
    VALUES (@Name, @Description, @SortOrder, @IsActive, ISNULL(@CreatedAt, SYSUTCDATETIME()), @ThreadCount, @PostCount, @LastPostByUserId, @LastPostAt);
END;
GO
