using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using MvcBB.Shared.Models.Settings;

namespace MvcBB.App.Models
{
    public class DisplaySettingsViewModel : DisplaySettings
    {
        [Display(Name = "Upload New Logo")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "Remove Current Logo")]
        public bool RemoveLogo { get; set; }
    }
}
