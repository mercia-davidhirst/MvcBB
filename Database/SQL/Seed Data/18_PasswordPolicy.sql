/*
    Seeds dbo.PasswordPolicy with its one singleton row, matching
    MvcBB.Shared.Models.Settings.PasswordPolicy's own property defaults (the
    same values the old static-field-backed PasswordPolicyService used to
    fall back to). Only runs if the table is still empty, so re-running
    this script won't overwrite values an admin has since changed via
    Admin/Settings.
*/

IF NOT EXISTS (SELECT 1 FROM dbo.PasswordPolicy)
BEGIN
    INSERT INTO dbo.PasswordPolicy (Id, MinimumLength, RequireUppercase, RequireLowercase, RequireDigit, RequireSpecialCharacter)
    VALUES
        (1, 8, 0, 0, 0, 0);
END;
GO
