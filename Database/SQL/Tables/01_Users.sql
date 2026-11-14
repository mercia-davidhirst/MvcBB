/*
    Table:   dbo.Users
    Purpose: Backing store for MvcBB.Shared.Models.User.User / IUserRepository.
    Notes:
      - Username/Email lookups in IUserRepository (GetByUsername, GetByEmail,
        ExistsWithUsername, ExistsWithEmail) are case-insensitive in the app
        (StringComparison.OrdinalIgnoreCase); this relies on the database/column
        using a case-insensitive collation (SQL Server's default CI_AS collations
        already satisfy this).
      - ThreadCount/PostCount on the User model are NOT persisted here: the
        repository exposes CountPostsByUsername/CountThreadsByUsername as
        computed queries (see usp_User_CountPostsByUsername /
        usp_User_CountThreadsByUsername), so storing running totals would just
        be a duplicate of derivable data.
      - Role mirrors MvcBB.Shared.Models.User.UserRole: 0 = User, 1 = Moderator,
        2 = Administrator, 3 = Deleted. A Deleted row has had Username/Email/
        PasswordHash/Signature/Bio/AvatarUrl overwritten with generic
        placeholders by UsersController's Anonymize action (via the ordinary
        usp_User_Update), not by a dedicated procedure.
      - CustomTitle is an admin-only override shown instead of the user's
        computed rank (RankCalculator.GetRank) - NULL means "use the computed
        rank", which is the default for everyone. Set via
        UsersController.SetCustomTitle, never by the user themselves.
      - MustChangePassword/LockedUntil/LastLoginIp are all admin-facing account
        controls (UsersController.RequirePasswordChange/LockUser, and Login
        writing LastLoginIp) - see IUserRepository's Add/Update for how they're
        persisted.
*/

IF OBJECT_ID('dbo.Users') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id              INT IDENTITY(1,1)  NOT NULL,
        Username        NVARCHAR(50)       NOT NULL,
        PasswordHash    NVARCHAR(256)      NOT NULL,
        Email           NVARCHAR(256)      NOT NULL,
        CreatedAt       DATETIME2(3)       NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        LastLoginAt     DATETIME2(3)       NULL,
        Role            TINYINT            NOT NULL CONSTRAINT DF_Users_Role DEFAULT (0),
        Signature       NVARCHAR(1000)     NULL,
        Bio             NVARCHAR(2000)     NULL,
        AvatarUrl       NVARCHAR(500)      NULL,
        ShowEmail       BIT                NOT NULL CONSTRAINT DF_Users_ShowEmail DEFAULT (0),
        CustomTitle     NVARCHAR(70)       NULL,
        MustChangePassword BIT             NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT (0),
        LockedUntil     DATETIME2(3)       NULL,
        LastLoginIp     NVARCHAR(45)       NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT CK_Users_Role CHECK (Role IN (0, 1, 2, 3))
    );
END
GO

-- Schema changes for a database that already has an older version of this
-- table (a fresh install gets the up-to-date shape directly from
-- CREATE TABLE above, so these only ever run against an existing table):
-- none pending yet - add guarded ALTER TABLE statements here, e.g.:
--   IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'NewColumn')
--   BEGIN
--       ALTER TABLE dbo.Users ADD NewColumn ...;
--   END
--   GO
