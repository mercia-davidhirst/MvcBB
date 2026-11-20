-- Matches IModRepository.Upsert(...) - discovery/metadata only. Never touches
-- IsEnabled (admin-controlled, see usp_Mod_SetEnabled) or InstalledVersion
-- (set only after a successful IApiMod.InstallAsync, see usp_Mod_SetInstalledVersion).

DROP PROCEDURE IF EXISTS dbo.usp_Mod_Upsert;
GO

CREATE PROCEDURE dbo.usp_Mod_Upsert
    @Id NVARCHAR(100),
    @Name NVARCHAR(200),
    @Version NVARCHAR(50),
    @MinCoreVersion NVARCHAR(50),
    @MaxCoreVersion NVARCHAR(50) = NULL,
    @Status NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Mods WHERE Id = @Id)
    BEGIN
        UPDATE dbo.Mods
        SET Name = @Name,
            Version = @Version,
            MinCoreVersion = @MinCoreVersion,
            MaxCoreVersion = @MaxCoreVersion,
            Status = @Status
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Mods (Id, Name, Version, MinCoreVersion, MaxCoreVersion, InstalledVersion, IsEnabled, Status, DiscoveredAt)
        VALUES (@Id, @Name, @Version, @MinCoreVersion, @MaxCoreVersion, NULL, 0, @Status, SYSUTCDATETIME());
    END

    SELECT Id, Name, Version, MinCoreVersion, MaxCoreVersion, InstalledVersion, IsEnabled, Status, DiscoveredAt
    FROM dbo.Mods
    WHERE Id = @Id;
END;
GO
