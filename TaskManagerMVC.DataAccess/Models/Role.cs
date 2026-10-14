using System.ComponentModel.DataAnnotations;

namespace TaskManagerMVC.DataAccess.Models
{
    /// <summary>
    /// Role entity for role-based authorization
    /// </summary>
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        public virtual ICollection<ApplicationUser>? Users { get; set; }
    }
}