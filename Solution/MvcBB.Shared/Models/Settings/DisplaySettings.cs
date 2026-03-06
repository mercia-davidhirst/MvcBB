using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.Settings
{
    public class DisplaySettings
    {
        [Range(1, 100, ErrorMessage = "Threads per page must be between 1 and 100")]
        public int ThreadsPerPage { get; set; } = 20;
    }
}
