/*
    Function: dbo.ufn_Report_ContentExists
    Purpose:  Reports.ContentId is a polymorphic reference whose meaning
              depends on Reports.Type (0 = Thread, 1 = Post, 2 = Message,
              3 = User, 4 = SiteIssue), so
              it cannot be enforced with a normal foreign key. This function
              checks the row actually exists in the table implied by @Type,
              and is used by usp_Report_Insert.
*/

DROP FUNCTION IF EXISTS dbo.ufn_Report_ContentExists;
GO

CREATE FUNCTION dbo.ufn_Report_ContentExists (@Type TINYINT, @ContentId INT)
RETURNS BIT
AS
BEGIN
    DECLARE @Exists BIT = 0;

    IF @Type = 0 -- Thread
        SELECT @Exists = 1 FROM dbo.Threads WHERE Id = @ContentId;
    ELSE IF @Type = 1 -- Post
        SELECT @Exists = 1 FROM dbo.Posts WHERE Id = @ContentId;
    ELSE IF @Type = 2 -- Message
        SELECT @Exists = 1 FROM dbo.Messages WHERE Id = @ContentId;
    ELSE IF @Type = 3 -- User
        SELECT @Exists = 1 FROM dbo.Users WHERE Id = @ContentId;
    ELSE IF @Type = 4 -- SiteIssue: about the site, not any content, so ContentId must be 0
        SET @Exists = CASE WHEN @ContentId = 0 THEN 1 ELSE 0 END;

    RETURN ISNULL(@Exists, 0);
END;
GO
