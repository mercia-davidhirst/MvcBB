-- Matches IModerationSettingsRepository.Get()

DROP PROCEDURE IF EXISTS dbo.usp_ModerationSettings_Get;
GO

CREATE PROCEDURE dbo.usp_ModerationSettings_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT HoldNewMemberPosts, NewMemberPostThreshold
    FROM dbo.ModerationSettings
    WHERE Id = 1;
END;
GO
