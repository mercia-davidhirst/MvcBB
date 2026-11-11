/*
    Table:   dbo.Polls
    Purpose: Backing store for MvcBB.Shared.Models.Poll.Poll / IPollRepository.
    Notes:
      - One poll per thread (UNIQUE on ThreadId) - polls are attached only at
        thread-creation time and can't be added retroactively.
*/

DROP TABLE IF EXISTS dbo.Polls;
GO

CREATE TABLE dbo.Polls
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    ThreadId        INT                NOT NULL,
    Question        NVARCHAR(200)      NOT NULL,
    CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_Polls_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Polls PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Polls_ThreadId UNIQUE (ThreadId),
    CONSTRAINT FK_Polls_Thread FOREIGN KEY (ThreadId) REFERENCES dbo.Threads (Id)
);
GO
