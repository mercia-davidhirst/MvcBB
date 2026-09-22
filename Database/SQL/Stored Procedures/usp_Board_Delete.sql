-- Matches IBoardRepository.Remove(Board board)

DROP PROCEDURE IF EXISTS dbo.usp_Board_Delete;
GO

CREATE PROCEDURE dbo.usp_Board_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Boards
    WHERE Id = @Id;
END;
GO
