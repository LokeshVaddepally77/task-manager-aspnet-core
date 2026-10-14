using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerMVC.DataAccess.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required]
        public int TaskId { get; set; }

        [Required]
        public int UserId { get; set; }

        // Navigation Properties
        [ForeignKey("TaskId")]
        public virtual TaskItem? Task { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}