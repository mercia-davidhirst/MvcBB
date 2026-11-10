using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Poll
{
    public class VotePollRequest
    {
        [Required]
        public int PollOptionId { get; set; }
    }
}
