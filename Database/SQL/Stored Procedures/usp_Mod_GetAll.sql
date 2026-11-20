-- Matches IModRepository.GetAll()

DROP PROCEDURE IF EXISTS dbo.usp_Mod_GetAll;
GO

CREATE PROCEDURE dbo.usp_Mod_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Version, MinCoreVersion, MaxCoreVersion, InstalledVersion, IsEnabled, Status, DiscoveredAt
    FROM dbo.Mods
    ORDER BY Name;
END;
GO
