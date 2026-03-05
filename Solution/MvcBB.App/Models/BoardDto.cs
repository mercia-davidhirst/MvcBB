using System;

namespace MvcBB.App.Models
{
    public class BoardDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }
        public string LastPostByUserId { get; set; }
        public DateTime? LastPostAt { get; set; }
    }

    public class CreateBoardDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdateBoardDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
} 