using System.ComponentModel.DataAnnotations;

namespace MvcBB.Shared.Models.User
{
    public class UpdateUserRoleRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [EnumDataType(typeof(UserRole))]
        public UserRole NewRole { get; set; }
    }
} 