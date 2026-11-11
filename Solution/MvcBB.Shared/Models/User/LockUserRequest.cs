namespace MvcBB.Shared.Models.User
{
    public class LockUserRequest
    {
        /// <summary>Null or in the past unlocks the account; a future value locks it until then.</summary>
        public DateTime? LockedUntil { get; set; }
    }
}
