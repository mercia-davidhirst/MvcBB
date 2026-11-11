using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using MvcBB.Shared.Models.Rank;

namespace MvcBB.App.Models
{
    public class RankViewModel : RankModel
    {
        [Display(Name = "Image File")]
        public IFormFile? ImageFile { get; set; }
    }
}
