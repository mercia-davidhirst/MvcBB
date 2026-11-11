/*
    Seeds dbo.Smilies with the default smilie set currently hardcoded in
    MvcBB.App.Services.BBCodeService's constructor. Only runs if the table is
    still empty, so re-running this script won't resurrect rows an admin has
    since edited or deleted via Admin/Smilies.
*/

IF NOT EXISTS (SELECT 1 FROM dbo.Smilies)
BEGIN
    INSERT INTO dbo.Smilies (Code, Description, ImagePath, IsActive, SortOrder)
    VALUES
        (N':)', N'Smile', N'/images/smilies/smile.gif', 1, 1),
        (N':-)', N'Smile', N'/images/smilies/smile.gif', 1, 2),
        (N':(', N'Sad', N'/images/smilies/sad.gif', 1, 3),
        (N':-(', N'Sad', N'/images/smilies/sad.gif', 1, 4),
        (N':D', N'Grin', N'/images/smilies/grin.gif', 1, 5),
        (N':-D', N'Grin', N'/images/smilies/grin.gif', 1, 6),
        (N';)', N'Wink', N'/images/smilies/wink.gif', 1, 7),
        (N';-)', N'Wink', N'/images/smilies/wink.gif', 1, 8),
        (N':P', N'Tongue', N'/images/smilies/tongue.gif', 1, 9),
        (N':-P', N'Tongue', N'/images/smilies/tongue.gif', 1, 10),
        (N':O', N'Surprised', N'/images/smilies/surprised.gif', 1, 11),
        (N':-O', N'Surprised', N'/images/smilies/surprised.gif', 1, 12);
END;
GO
