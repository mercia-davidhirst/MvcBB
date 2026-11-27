-- Matches IDisplaySettingsRepository.Get()

DROP PROCEDURE IF EXISTS dbo.usp_DisplaySettings_Get;
GO

CREATE PROCEDURE dbo.usp_DisplaySettings_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ThreadsPerPage, SiteTitle, WelcomeMessage, SiteName, LogoPath, FooterDisclaimer, RegistrationEnabled, DateFormat, TimeFormat, TimeZoneId,
           MaxEmailsPerHour, MaxEmailsPerDay, PostsPerPage, ShowWhosOnline, OnlineWindowMinutes
    FROM dbo.DisplaySettings
    WHERE Id = 1;
END;
GO
