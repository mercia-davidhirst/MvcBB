-- Matches IModerationSettingsRepository.Update(ModerationSettings settings)
-- Creates the singleton row if the seed script hasn't run yet.

DROP PROCEDURE IF EXISTS dbo.usp_ModerationSettings_Update;
GO

CREATE PROCEDURE dbo.usp_ModerationSettings_Update
    @HoldNewMemberPosts BIT,
    @NewMemberPostThreshold INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ModerationSettings
    SET HoldNewMemberPosts = @HoldNewMemberPosts,
        NewMemberPostThreshold = @NewMemberPostThreshold
    WHERE Id = 1;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO dbo.ModerationSettings (Id, HoldNewMemberPosts, NewMemberPostThreshold)
        VALUES (1, @HoldNewMemberPosts, @NewMemberPostThreshold);
    END
END;
GO
