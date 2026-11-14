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

IF OBJECT_ID('dbo.Announcements') IS NULL
BEGIN
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

    CREATE NONCLUSTERED INDEX IX_Announcements_SortOrder ON dbo.Announcements (SortOrder);
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Announcements') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Announcements ADD NewColumn ...;
--   END
--   GO
