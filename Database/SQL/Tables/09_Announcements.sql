/*
    Table:   dbo.Announcements
    Purpose: Backing store for MvcBB.Shared.Models.Announcement.AnnouncementModel /
             IAnnouncementManagementService (GetAnnouncements, GetActiveAnnouncements,
             GetAnnouncement, AddAnnouncement, UpdateAnnouncement, DeleteAnnouncement).
    Notes:
      - StartDate/EndDate are optional scheduling bounds - NULL means "no lower/upper
        bound". GetActive (usp_Announcement_GetActive) is what the portal page and
        any other public surface should call, since it already applies IsActive plus
        the StartDate/EndDate window.
*/

DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
INNER JOIN sys.tables AS t ON t.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('dbo.Announcements');

IF LEN(@sql) > 0
    EXEC sp_executesql @sql;
GO

DROP TABLE IF EXISTS dbo.Announcements;
GO

CREATE TABLE dbo.Announcements
(
    Id              INT IDENTITY(1,1)  NOT NULL,
    Title           NVARCHAR(200)      NOT NULL,
    Content         NVARCHAR(MAX)      NOT NULL,
    IsActive        BIT                NOT NULL CONSTRAINT DF_Announcements_IsActive DEFAULT (1),
    SortOrder       INT                NOT NULL CONSTRAINT DF_Announcements_SortOrder DEFAULT (0),
    StartDate       DATETIME2          NULL,
    EndDate         DATETIME2          NULL,
    CreatedAt       DATETIME2          NOT NULL CONSTRAINT DF_Announcements_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Announcements PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE NONCLUSTERED INDEX IX_Announcements_SortOrder ON dbo.Announcements (SortOrder);
GO
