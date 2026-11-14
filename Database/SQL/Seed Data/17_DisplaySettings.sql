/*
    Seeds dbo.DisplaySettings with its one singleton row, matching
    MvcBB.Shared.Models.Settings.DisplaySettings' own property defaults
    (the same values the old static-field-backed DisplaySettingsService
    used to fall back to). Only runs if the table is still empty, so
    re-running this script won't overwrite values an admin has since
    changed via Admin/Settings.
*/

IF NOT EXISTS (SELECT 1 FROM dbo.DisplaySettings)
BEGIN
    INSERT INTO dbo.DisplaySettings (Id, ThreadsPerPage, SiteTitle, WelcomeMessage, SiteName, LogoPath, FooterDisclaimer, RegistrationEnabled, DateFormat, TimeFormat)
    VALUES
        (1, 20, N'Welcome to MvcBB', N'A modern bulletin board system for engaging discussions and community building.', N'MvcBB', NULL, NULL, 1, N'dd/MM/yyyy', N'HH:mm');
END;
GO
