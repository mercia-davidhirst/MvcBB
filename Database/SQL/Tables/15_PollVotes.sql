/*
    Table:   dbo.PollVotes
    Purpose: Backing store for MvcBB.Shared.Models.Poll.PollVote /
             IPollRepository - one row per (poll, user) vote.
    Notes:
      - UNIQUE on (PollId, UserId) enforces single-choice voting at the DB
        level too, matching IPollRepository.AddVote's HasVoted guard.
*/

DROP TABLE IF EXISTS dbo.PollVotes;
GO

CREATE TABLE dbo.PollVotes
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    PollId          INT                NOT NULL,
    PollOptionId    INT                NOT NULL,
    UserId          INT                NOT NULL,
    CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_PollVotes_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_PollVotes PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_PollVotes_Poll_User UNIQUE (PollId, UserId),
    CONSTRAINT FK_PollVotes_Poll FOREIGN KEY (PollId) REFERENCES dbo.Polls (Id),
    CONSTRAINT FK_PollVotes_PollOption FOREIGN KEY (PollOptionId) REFERENCES dbo.PollOptions (Id),
    CONSTRAINT FK_PollVotes_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id)
);
GO

CREATE NONCLUSTERED INDEX IX_PollVotes_PollId ON dbo.PollVotes (PollId);
GO
