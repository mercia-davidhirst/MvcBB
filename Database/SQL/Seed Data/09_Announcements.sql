/*
    Seeds dbo.Announcements with a default welcome announcement, matching
    MvcBB.API.InMemory.InMemoryStore's SeedAnnouncements(). Only runs if the
    table is still empty, so re-running this script won't resurrect a row an
    admin has since edited or deleted via Admin/Announcements.
*/

IF NOT EXISTS (SELECT 1 FROM dbo.Announcements)
BEGIN
    INSERT INTO dbo.Announcements (Title, Content, IsActive, SortOrder, StartDate, EndDate, CreatedAt)
    VALUES
        (N'Welcome to MvcBB!', N'Welcome to the forums :) Please read the rules before posting.', 1, 1, NULL, NULL, SYSUTCDATETIME());
END;
GO
