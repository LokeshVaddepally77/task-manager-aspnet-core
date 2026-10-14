using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TaskManagerMVC.DataAccess.Models
{
    /// <summary>
    /// Custom User class extending IdentityUser
    /// Stores additional information about registered users
    /// </summary>
    public class ApplicationUser : IdentityUser<int>
    {
        // ==================== ADDITIONAL USER PROPERTIES ====================

        [StringLength(50)]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        // ==================== REQUIRED PROPERTIES (Gender, City, PostalCode) ====================

        [StringLength(10)]
        [Display(Name = "Gender")]
        public string? Gender { get; set; }  // Male, Female, Other, Prefer not to say

        [StringLength(100)]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // ==================== APPLICATION SPECIFIC PROPERTIES ====================

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Employee";  // Manager or Employee

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLoginDate { get; set; }

        // Google OAuth identifier
        [StringLength(255)]
        public string? GoogleId { get; set; }

        // Profile Picture URL (optional)
        [StringLength(500)]
        public string? ProfilePictureUrl { get; set; }

        // ==================== NAVIGATION PROPERTIES ====================

        public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();

        public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();

        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        // ==================== COMPUTED PROPERTIES ====================

        [NotMapped]
        public string FullName => string.IsNullOrWhiteSpace(FirstName)
            ? UserName ?? "Unknown"
            : $"{FirstName} {LastName}".Trim();

        [NotMapped]
        public bool IsManager => Role == "Manager";

        [NotMapped]
        public bool IsGoogleUser => !string.IsNullOrEmpty(GoogleId);

        //  maps to IdentityUser's UserName
        [NotMapped]
        public string Username
        {
            get => UserName ?? string.Empty;
            set => UserName = value;
        }

        // maps to IdentityUser's Id
        [NotMapped]
        public int UserId
        {
            get => Id;
            set => Id = value;
        }
    }

    /// <summary>
    /// Custom Role class extending IdentityRole
    /// </summary>
    public class ApplicationRole : IdentityRole<int>
    {
        public string? Description { get; set; }
    }
}