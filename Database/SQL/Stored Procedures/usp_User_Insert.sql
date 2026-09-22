-- Matches IUserRepository.Add(User user)

DROP PROCEDURE IF EXISTS dbo.usp_User_Insert;
GO

CREATE PROCEDURE dbo.usp_User_Insert
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(256),
    @Email NVARCHAR(256),
    @CreatedAt DATETIME2(3) = NULL,
    @Role TINYINT = 0,
    @Signature NVARCHAR(1000) = NULL,
    @Bio NVARCHAR(2000) = NULL,
    @AvatarUrl NVARCHAR(500) = NULL,
    @ShowEmail BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Users (Username, PasswordHash, Email, CreatedAt, Role, Signature, Bio, AvatarUrl, ShowEmail)
    OUTPUT inserted.Id, inserted.Username, inserted.PasswordHash, inserted.Email, inserted.CreatedAt,
           inserted.LastLoginAt, inserted.Role, inserted.Signature, inserted.Bio, inserted.AvatarUrl, inserted.ShowEmail
    VALUES (@Username, @PasswordHash, @Email, ISNULL(@CreatedAt, SYSUTCDATETIME()), @Role, @Signature, @Bio, @AvatarUrl, @ShowEmail);
END;
GO
