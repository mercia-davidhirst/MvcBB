using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.WordFilter
{
    /// <summary>
    /// An admin-managed censor entry: Word is matched case-insensitively as a
    /// whole word (so "class" doesn't match inside "classic") and swapped for
    /// Replacement. Applied once, at write time, to a post's Content (and a
    /// thread's Title) before it's saved - not at render time - so the stored
    /// data is already clean and every future read (API, search, ETL export)
    /// sees the same text a visitor does.
    /// </summary>
    public class WordFilterModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Word is required")]
        [StringLength(100, ErrorMessage = "Word cannot exceed 100 characters")]
        public string Word { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Replacement cannot exceed 100 characters")]
        public string Replacement { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }
    }
}
