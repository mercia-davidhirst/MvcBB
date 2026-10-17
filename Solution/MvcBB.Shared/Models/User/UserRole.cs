namespace MvcBB.Shared.Models.User
{
    public enum UserRole
    {
        User = 0,
        Moderator = 1,
        Administrator = 2,

        /// <summary>
        /// An anonymized account (banned or removal-requested): Username/Email/
        /// PasswordHash/Signature/Bio/AvatarUrl have been overwritten with
        /// generic placeholders so historical posts/threads stay intact without
        /// exposing the original identity. Not reachable via the normal role
        /// dropdown - only via the dedicated anonymize action.
        /// </summary>
        Deleted = 3
    }
} 