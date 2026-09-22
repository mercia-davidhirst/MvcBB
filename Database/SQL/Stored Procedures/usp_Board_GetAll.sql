-- Matches IBoardRepository.GetAll() (ordered by SortOrder, as in InMemoryBoardRepository)

DROP PROCEDURE IF EXISTS dbo.usp_Board_GetAll;
GO

CREATE PROCEDURE dbo.usp_Board_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Description, SortOrder, IsActive, CreatedAt, UpdatedAt,
           ThreadCount, PostCount, LastPostByUserId, LastPostAt
    FROM dbo.Boards
    ORDER BY SortOrder;
END;
GO
