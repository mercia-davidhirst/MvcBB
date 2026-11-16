using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.ForumThread
{
    public class ForumThread
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        public int BoardId { get; set; }
        public required string CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsSticky { get; set; }
        public bool IsLocked { get; set; }
        public bool IsImportedData { get; set; }

        public int ViewCount { get; set; }
        public int PostCount { get; set; }

        // Stored instead of computed on the fly per request - TotalLikes is
        // maintained exclusively by the usp_PostLike_*/sp_post_like_* procs
        // (never by the generic Update path, to avoid a concurrent like/unlike
        // being clobbered by a stale snapshot); ParticipantCount is maintained
        // by ThreadsController/PostsController, matching ViewCount/PostCount.
        public int TotalLikes { get; set; }
        public int ParticipantCount { get; set; }

        public DateTime? LastPostAt { get; set; }
        public string? LastPostByUserId { get; set; }

        // Denormalized via a join at the data layer (see dbo.vw_ThreadDetails /
        // public.vw_thread_details), matching the same pattern Post already uses
        // for ThreadTitle/CreatedByUsername - so listing threads never needs a
        // separate board or username lookup per thread.
        public string BoardName { get; set; } = string.Empty;
        public bool BoardIsPrivate { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        public string? LastPostByUsername { get; set; }
    }
} 