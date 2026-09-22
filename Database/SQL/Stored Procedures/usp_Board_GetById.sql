-- Matches IBoardRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Board_GetById;
GO

CREATE PROCEDURE dbo.usp_Board_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, SortOrder, IsActive, CreatedAt, UpdatedAt,
           ThreadCount, PostCount, LastPostByUserId, LastPostAt
    FROM dbo.Boards
    WHERE Id = @Id;
END;
GO
