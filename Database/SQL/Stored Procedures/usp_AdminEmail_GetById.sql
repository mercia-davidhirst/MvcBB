-- Matches IAdminEmailRepository.GetById(int id)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmail_GetById;
GO

CREATE PROCEDURE dbo.usp_AdminEmail_GetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.AdminEmails WHERE Id = @Id;
END;
GO
