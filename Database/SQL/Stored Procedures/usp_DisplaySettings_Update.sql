-- Matches IDisplaySettingsRepository.Update(DisplaySettings settings)

DROP PROCEDURE IF EXISTS dbo.usp_DisplaySettings_Update;
GO

CREATE PROCEDURE dbo.usp_DisplaySettings_Update
    @ThreadsPerPage INT,
    @SiteTitle NVARCHAR(100),
    @WelcomeMessage NVARCHAR(500),
    @SiteName NVARCHAR(50),
    @LogoPath NVARCHAR(500) = NULL,
    @FooterDisclaimer NVARCHAR(500) = NULL,
    @RegistrationEnabled BIT,
    @DateFormat NVARCHAR(20),
    @TimeFormat NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DisplaySettings
    SET ThreadsPerPage = @ThreadsPerPage,
        SiteTitle = @SiteTitle,
        WelcomeMessage = @WelcomeMessage,
        SiteName = @SiteName,
        LogoPath = @LogoPath,
        FooterDisclaimer = @FooterDisclaimer,
        RegistrationEnabled = @RegistrationEnabled,
        DateFormat = @DateFormat,
        TimeFormat = @TimeFormat
    WHERE Id = 1;
END;
GO
