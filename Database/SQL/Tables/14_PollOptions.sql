/*
    Table:   dbo.PollOptions
    Purpose: Backing store for MvcBB.Shared.Models.Poll.PollOption /
             IPollRepository.
*/

DROP TABLE IF EXISTS dbo.PollOptions;
GO

CREATE TABLE dbo.PollOptions
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    PollId          INT                NOT NULL,
    Text            NVARCHAR(200)      NOT NULL,
    SortOrder       INT                NOT NULL CONSTRAINT DF_PollOptions_SortOrder DEFAULT (0),
    CONSTRAINT PK_PollOptions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PollOptions_Poll FOREIGN KEY (PollId) REFERENCES dbo.Polls (Id)
);
GO

CREATE NONCLUSTERED INDEX IX_PollOptions_PollId ON dbo.PollOptions (PollId);
GO
