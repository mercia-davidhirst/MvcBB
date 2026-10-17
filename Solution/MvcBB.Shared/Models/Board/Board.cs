using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Board
{
    public class Board
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Board name is required")]
        [StringLength(100, ErrorMessage = "Board name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Parent board for nested categories/sub-boards (WBB-style hierarchy).
        /// Null means this is a top-level board.
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// True for a pure organizational category header with no threads of
        /// its own (mirrors WBB's "isboard" flag); false for a real board.
        /// </summary>
        public bool IsCategory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
        public string LastPostByUserId { get; set; }
        public DateTime? LastPostAt { get; set; }
    }
} 