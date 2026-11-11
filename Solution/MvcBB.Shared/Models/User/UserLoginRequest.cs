using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class UserLoginRequest
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Set by MvcBB.App's AccountController (never by the browser directly)
        /// from the request that reached the App, since MvcBB.API only ever
        /// sees the App server's own IP, not the end user's.
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
