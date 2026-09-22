-- Matches IUserRepository.Update(User user)

DROP PROCEDURE IF EXISTS dbo.usp_User_Update;
GO

CREATE PROCEDURE dbo.usp_User_Update
    @Id INT,
    @Username NVARCHAR(50),
    @PasswordHash NVARCHAR(256),
    @Email NVARCHAR(256),
    @LastLoginAt DATETIME2(3) = NULL,
    @Role TINYINT,
    @Signature NVARCHAR(1000) = NULL,
    @Bio NVARCHAR(2000) = NULL,
    @AvatarUrl NVARCHAR(500) = NULL,
    @ShowEmail BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET Username = @Username,
        PasswordHash = @PasswordHash,
        Email = @Email,
        LastLoginAt = @LastLoginAt,
        Role = @Role,
        Signature = @Signature,
        Bio = @Bio,
        AvatarUrl = @AvatarUrl,
        ShowEmail = @ShowEmail
    WHERE Id = @Id;
END;
GO
