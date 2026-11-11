using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class AdminSetEmailRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;
    }
}
