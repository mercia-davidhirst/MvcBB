using System.ComponentModel.DataAnnotations;
using MvcBB.Shared.Models.Poll;

namespace MvcBB.Shared.Models.ForumThread
{
    public class CreateForumThreadRequest
    {
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }

        [Required]
        public int BoardId { get; set; }

        [Required]
        public required string Content { get; set; }

        /// <summary>
        /// Optional poll attached at thread-creation time only - matches
        /// classic forum behavior where polls can't be added retroactively.
        /// </summary>
        public CreatePollRequest? Poll { get; set; }
    }
}
