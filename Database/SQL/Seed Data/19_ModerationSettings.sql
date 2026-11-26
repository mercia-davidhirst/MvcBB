/*
    Seeds dbo.ModerationSettings with its one singleton row, matching
    MvcBB.Shared.Models.Moderation.ModerationSettings' own defaults (holding
    new members' posts is off). Only runs if the table is still empty, so
    re-running this script won't overwrite values an admin has since changed
    on the Moderation settings page.
*/

IF NOT EXISTS (SELECT 1 FROM dbo.ModerationSettings)
BEGIN
    INSERT INTO dbo.ModerationSettings (Id, HoldNewMemberPosts, NewMemberPostThreshold)
    VALUES (1, 0, 1);
END;
GO
