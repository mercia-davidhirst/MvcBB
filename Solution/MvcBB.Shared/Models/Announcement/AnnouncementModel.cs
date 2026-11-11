using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Announcement
{
    public class AnnouncementModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
