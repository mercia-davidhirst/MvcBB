-- Matches IModRepository.SetInstalledVersion(string id, string version) -
-- called once IApiMod.InstallAsync has completed successfully for that version.

DROP PROCEDURE IF EXISTS dbo.usp_Mod_SetInstalledVersion;
GO

CREATE PROCEDURE dbo.usp_Mod_SetInstalledVersion
    @Id NVARCHAR(100),
    @Version NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Mods
    SET InstalledVersion = @Version
    WHERE Id = @Id;
END;
GO
