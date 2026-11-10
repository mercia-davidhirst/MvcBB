using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Poll
{
    public class CreatePollRequest
    {
        [Required(ErrorMessage = "Poll question is required")]
        [StringLength(200, ErrorMessage = "Poll question cannot exceed 200 characters")]
        public string Question { get; set; } = string.Empty;

        [MinLength(2, ErrorMessage = "A poll needs at least 2 options")]
        public List<string> Options { get; set; } = new();
    }
}
