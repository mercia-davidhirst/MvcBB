/*
    Table:   dbo.Reports
    Purpose: Backing store for MvcBB.Shared.Models.Report.Report /
             IReportRepository.
    Notes:
      - Type mirrors ReportType: 0 = Thread, 1 = Post, 2 = Message.
      - Status mirrors ReportStatus: 0 = Pending, 1 = Investigating,
        2 = Resolved, 3 = Dismissed.
      - ContentId is a polymorphic reference (its meaning depends on Type), so
        it cannot be a real foreign key; dbo.ufn_Report_ContentExists is used
        by usp_Report_Insert to validate it against the right table instead.
*/

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Reports');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Reports;
GO

CREATE TABLE dbo.Reports
(
    Id                  INT IDENTITY(1,1)  NOT NULL,
    Reason              NVARCHAR(500)      NOT NULL,
    ReporterUserId      INT                NOT NULL,
    CreatedAt           DATETIME2(3)       NOT NULL CONSTRAINT DF_Reports_CreatedAt DEFAULT (SYSUTCDATETIME()),
    Type                TINYINT            NOT NULL,
    ContentId           INT                NOT NULL,
    Status               TINYINT           NOT NULL CONSTRAINT DF_Reports_Status DEFAULT (0),
    ModeratorNotes      NVARCHAR(1000)     NOT NULL CONSTRAINT DF_Reports_ModeratorNotes DEFAULT (N''),
    ResolvedAt          DATETIME2(3)       NULL,
    ResolvedByUserId    INT                NULL,
    CONSTRAINT PK_Reports PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Reports_ReporterUser FOREIGN KEY (ReporterUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT FK_Reports_ResolvedByUser FOREIGN KEY (ResolvedByUserId) REFERENCES dbo.Users (Id),
    CONSTRAINT CK_Reports_Type CHECK (Type IN (0, 1, 2)),
    CONSTRAINT CK_Reports_Status CHECK (Status IN (0, 1, 2, 3))
);
GO

CREATE NONCLUSTERED INDEX IX_Reports_CreatedAt ON dbo.Reports (CreatedAt DESC);
GO

CREATE NONCLUSTERED INDEX IX_Reports_Status ON dbo.Reports (Status);
GO

CREATE NONCLUSTERED INDEX IX_Reports_Type ON dbo.Reports (Type);
GO
