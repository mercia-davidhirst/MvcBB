namespace MvcBB.Shared.Models.Poll
{
    public class PollResponse
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public string Question { get; set; } = string.Empty;
        public List<PollOptionResponse> Options { get; set; } = new();
        public int TotalVotes { get; set; }

        // Additional properties for UI display - per the requesting user
        public bool HasVoted { get; set; }
        public int? VotedOptionId { get; set; }
    }

    public class PollOptionResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int VoteCount { get; set; }
    }
}
