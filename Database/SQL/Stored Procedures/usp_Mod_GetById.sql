-- Matches IModRepository.GetById(string id)

DROP PROCEDURE IF EXISTS dbo.usp_Mod_GetById;
GO

CREATE PROCEDURE dbo.usp_Mod_GetById
    @Id NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Name, Version, MinCoreVersion, MaxCoreVersion, InstalledVersion, IsEnabled, Status, DiscoveredAt
    FROM dbo.Mods
    WHERE Id = @Id;
END;
GO
