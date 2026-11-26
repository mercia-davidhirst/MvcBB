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
    @TimeFormat NVARCHAR(20),
    @TimeZoneId NVARCHAR(100) = N'',
    @MaxEmailsPerHour INT = 0,
    @MaxEmailsPerDay INT = 0,
    @PostsPerPage INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DisplaySettings
    SET ThreadsPerPage = @ThreadsPerPage,
        PostsPerPage = ISNULL(@PostsPerPage, 20),
        SiteTitle = @SiteTitle,
        WelcomeMessage = @WelcomeMessage,
        SiteName = @SiteName,
        LogoPath = @LogoPath,
        FooterDisclaimer = @FooterDisclaimer,
        RegistrationEnabled = @RegistrationEnabled,
        DateFormat = @DateFormat,
        TimeFormat = @TimeFormat,
        TimeZoneId = ISNULL(@TimeZoneId, N''),
        MaxEmailsPerHour = ISNULL(@MaxEmailsPerHour, 0),
        MaxEmailsPerDay = ISNULL(@MaxEmailsPerDay, 0)
    WHERE Id = 1;
END;
GO
