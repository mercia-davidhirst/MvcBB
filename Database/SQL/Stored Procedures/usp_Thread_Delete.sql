-- Matches IForumThreadRepository.Remove(ForumThread thread)
-- Deletes the thread and everything that belongs to it, in one transaction:
-- its posts and their likes, its poll (options and votes), watchers, read
-- positions (ON DELETE CASCADE), notifications about it and any replies to
-- it still waiting in the moderation queue. A post in another thread that
-- quoted one of these posts keeps its own text but loses the quote link
-- (QuotedPostId set to NULL), the same as quoting a post that was deleted.
-- Before v1.4.0 this only deleted the thread row, which failed with a
-- foreign key error (FK_Posts_Thread) for any thread that had posts.

DROP PROCEDURE IF EXISTS dbo.usp_Thread_Delete;
GO

CREATE PROCEDURE dbo.usp_Thread_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.Posts
    SET QuotedPostId = NULL
    WHERE ThreadId <> @Id
      AND QuotedPostId IN (SELECT Id FROM dbo.Posts WHERE ThreadId = @Id);

    DELETE FROM dbo.PostLikes
    WHERE PostId IN (SELECT Id FROM dbo.Posts WHERE ThreadId = @Id);

    DELETE FROM dbo.Posts WHERE ThreadId = @Id;

    DELETE v FROM dbo.PollVotes AS v
    INNER JOIN dbo.Polls AS p ON p.Id = v.PollId
    WHERE p.ThreadId = @Id;

    DELETE o FROM dbo.PollOptions AS o
    INNER JOIN dbo.Polls AS p ON p.Id = o.PollId
    WHERE p.ThreadId = @Id;

    DELETE FROM dbo.Polls WHERE ThreadId = @Id;
    DELETE FROM dbo.ThreadSubscriptions WHERE ThreadId = @Id;
    DELETE FROM dbo.Notifications WHERE ThreadId = @Id;
    DELETE FROM dbo.ModerationQueue WHERE ThreadId = @Id;

    DELETE FROM dbo.Threads WHERE Id = @Id;

    COMMIT TRANSACTION;
END;
GO
