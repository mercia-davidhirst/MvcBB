-- Matches IModRepository.SetEnabled(string id, bool enabled) - the admin
-- Mods page toggle.

DROP PROCEDURE IF EXISTS dbo.usp_Mod_SetEnabled;
GO

CREATE PROCEDURE dbo.usp_Mod_SetEnabled
    @Id NVARCHAR(100),
    @IsEnabled BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Mods
    SET IsEnabled = @IsEnabled
    WHERE Id = @Id;
END;
GO
