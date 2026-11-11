using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class SetCustomTitleRequest
    {
        /// <summary>Null or empty clears the override and falls back to the computed rank.</summary>
        [StringLength(70, ErrorMessage = "Title cannot exceed 70 characters")]
        public string? CustomTitle { get; set; }
    }
}
