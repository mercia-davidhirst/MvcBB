-- Matches IForumThreadRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetById;
GO

CREATE PROCEDURE dbo.usp_Thread_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, BoardId, BoardName, BoardIsPrivate, CreatedByUserId, CreatedByUsername,
           CreatedAt, UpdatedAt, IsSticky, IsLocked, ViewCount, PostCount, LastPostAt,
           LastPostByUserId, LastPostByUsername
    FROM dbo.vw_ThreadDetails
    WHERE Id = @Id;
END;
GO
