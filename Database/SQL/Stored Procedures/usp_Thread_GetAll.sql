-- Matches IForumThreadRepository.GetAll()

DROP PROCEDURE IF EXISTS dbo.usp_Thread_GetAll;
GO

CREATE PROCEDURE dbo.usp_Thread_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Title, BoardId, BoardName, BoardIsPrivate, CreatedByUserId, CreatedByUsername,
           CreatedAt, UpdatedAt, IsSticky, IsLocked, ViewCount, PostCount, LastPostAt,
           LastPostByUserId, LastPostByUsername
    FROM dbo.vw_ThreadDetails
    ORDER BY IsSticky DESC, ISNULL(LastPostAt, CreatedAt) DESC;
END;
GO
