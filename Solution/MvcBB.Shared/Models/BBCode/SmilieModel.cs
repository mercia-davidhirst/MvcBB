namespace MvcBB.Shared.Models.BBCode
{
    /// <summary>
    /// Represents a smilie emoticon with its code and image
    /// </summary>
    public class SmilieModel
    {
        public int Id { get; set; }
        
        public string Code { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string ImagePath { get; set; } = string.Empty;
        
        public bool IsActive { get; set; }
        
        public int SortOrder { get; set; }
    }
} 