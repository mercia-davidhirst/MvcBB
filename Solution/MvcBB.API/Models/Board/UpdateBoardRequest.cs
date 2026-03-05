using System.ComponentModel.DataAnnotations;

namespace MvcBB.API.Models.Board
{
    public class UpdateBoardRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
    }
} 