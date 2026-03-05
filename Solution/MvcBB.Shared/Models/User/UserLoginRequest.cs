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
    }
}
