using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Authorization
{
    public class ApiCredentials
    {
        [Required(ErrorMessage = "Client ID is required")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client Secret is required")]
        public string ClientSecret { get; set; } = string.Empty;
    }
} 