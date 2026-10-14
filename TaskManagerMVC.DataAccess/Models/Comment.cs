using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerMVC.DataAccess.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [Required(ErrorMessage = "Comment content is required")]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required]
        public int TaskId { get; set; }

        public int? UserId { get; set; }

        // Navigation Properties
        [ForeignKey("TaskId")]
        public virtual TaskItem? Task { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Computed Properties
        [NotMapped]
        public string AuthorName => User?.Username ?? "Anonymous";
    }
}