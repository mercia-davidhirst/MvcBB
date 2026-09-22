/*
    Function: dbo.ufn_Report_ContentExists
    Purpose:  Reports.ContentId is a polymorphic reference whose meaning
              depends on Reports.Type (0 = Thread, 1 = Post, 2 = Message), so
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

    RETURN ISNULL(@Exists, 0);
END;
GO
