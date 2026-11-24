-- Matches IAdminEmailRepository.MarkUnfinishedInterrupted() - run at API
-- startup: anything still Queued (0) or Sending (1) can't be in progress any
-- more, so it becomes Interrupted (4).

DROP PROCEDURE IF EXISTS dbo.usp_AdminEmail_MarkUnfinishedInterrupted;
GO

CREATE PROCEDURE dbo.usp_AdminEmail_MarkUnfinishedInterrupted
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.AdminEmails
    SET Status = 4,
        CompletedAt = SYSUTCDATETIME(),
        PausedUntil = NULL
    WHERE Status IN (0, 1);

    SELECT @@ROWCOUNT;
END;
GO
