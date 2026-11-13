-- Matches IDisplaySettingsRepository.Get()

DROP PROCEDURE IF EXISTS dbo.usp_DisplaySettings_Get;
GO

CREATE PROCEDURE dbo.usp_DisplaySettings_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ThreadsPerPage, SiteTitle, WelcomeMessage, SiteName, LogoPath, FooterDisclaimer, RegistrationEnabled
    FROM dbo.DisplaySettings
    WHERE Id = 1;
END;
GO
