using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Rank
{
    /// <summary>
    /// A post-count-based title (and optional badge image) shown next to a
    /// user's name. A user's rank is never stored against them directly -
    /// it's computed on the fly from their live post count against the
    /// highest MinPosts threshold they meet, so it always stays in sync with
    /// reality (see RankCalculator.GetRank).
    /// </summary>
    public class RankModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Rank title is required")]
        [StringLength(70, ErrorMessage = "Title cannot exceed 70 characters")]
        public string Title { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Minimum posts cannot be negative")]
        [Display(Name = "Minimum Posts")]
        public int MinPosts { get; set; }

        public string? ImagePath { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }
    }
}
