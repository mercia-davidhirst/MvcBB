namespace MvcBB.Shared.Models.Poll
{
    public class PollOption
    {
        public int Id { get; set; }
        public int PollId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
