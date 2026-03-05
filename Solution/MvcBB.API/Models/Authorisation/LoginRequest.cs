using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Authorisation
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
} 