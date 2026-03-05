using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using MvcBB.Shared.Models.BBCode;

namespace MvcBB.App.Models
{
    public class SmilieViewModel : SmilieModel
    {
        [Display(Name = "Image File")]
        public IFormFile? ImageFile { get; set; }
    }
} 