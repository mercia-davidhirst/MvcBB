-- Matches IModerationLogRepository.Add(ModerationLogEntry entry)

DROP PROCEDURE IF EXISTS dbo.usp_ModerationLog_Insert;
GO

CREATE PROCEDURE dbo.usp_ModerationLog_Insert
    @ActorUserId INT,
    @Action NVARCHAR(50),
    @TargetType NVARCHAR(20),
    @TargetId INT = NULL,
    @TargetUserId INT = NULL,
    @Summary NVARCHAR(500),
    @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ModerationLog (ActorUserId, Action, TargetType, TargetId, TargetUserId, Summary, Reason)
    VALUES (@ActorUserId, @Action, @TargetType, @TargetId, @TargetUserId, LEFT(@Summary, 500), LEFT(@Reason, 500));
END;
GO
