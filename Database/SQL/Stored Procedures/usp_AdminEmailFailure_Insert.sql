-- Matches IAdminEmailRepository.AddFailure(AdminEmailFailure failure)

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmailFailure_Insert;
GO

CREATE PROCEDURE dbo.usp_AdminEmailFailure_Insert
    @AdminEmailId INT,
    @UserId INT,
    @Username NVARCHAR(50),
    @Email NVARCHAR(256),
    @Reason NVARCHAR(1000),
    @FailedAt DATETIME2(3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AdminEmailFailures (AdminEmailId, UserId, Username, Email, Reason, FailedAt)
    VALUES (@AdminEmailId, @UserId, @Username, @Email, @Reason, ISNULL(@FailedAt, SYSUTCDATETIME()));
END;
GO
