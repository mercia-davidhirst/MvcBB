namespace MvcBB.Shared.Models.BBCode
{
    /// <summary>
    /// Represents a BBCode tag with its pattern and replacement
    /// </summary>
    public class BBCodeTagModel
    {
        public int Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string Pattern { get; set; } = string.Empty;
        
        public string Replacement { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string Example { get; set; } = string.Empty;
        
        public bool IsActive { get; set; }
        
        public int SortOrder { get; set; }
    }
} 