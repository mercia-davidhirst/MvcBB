-- Matches IPostRepository.Remove(Post post)

DROP PROCEDURE IF EXISTS dbo.usp_Post_Delete;
GO

CREATE PROCEDURE dbo.usp_Post_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Posts
    WHERE Id = @Id;
END;
GO
