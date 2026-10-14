using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerMVC.DataAccess.Models
{
    public class TaskItem
    {
        [Key]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Task title is required")]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(High|Medium|Low)$")]
        public string Priority { get; set; } = "Medium";

        [Required]
        [RegularExpression("^(New|In Progress|Completed|On Hold)$")]
        public string Status { get; set; } = "New";

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        [Range(0, 100)]
        [Display(Name = "Completion %")]
        public int CompletionPercentage { get; set; } = 0;

        // Foreign Keys
        [Required]
        public int CreatedById { get; set; }

        [Required]
        public int AssignedToId { get; set; }

        public int? CategoryId { get; set; }

        // Navigation Properties
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser? CreatedByUser { get; set; }

        [ForeignKey("AssignedToId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? CategoryNavigation { get; set; }

        public virtual ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public virtual ICollection<TaskHistory> TaskHistories { get; set; } = new List<TaskHistory>();

        // Computed Properties
        [NotMapped]
        public string AssignedTo => AssignedToUser?.Username ?? "Unassigned";

        [NotMapped]
        public string CreatedBy => CreatedByUser?.Username ?? "Unknown";

        [NotMapped]
        public string CategoryName => CategoryNavigation?.Name ?? "No Category";

        [NotMapped]
        public int CommentCount => Comments?.Count ?? 0;

        [NotMapped]
        public bool HasAttachments => Attachments?.Any() ?? false;

        [NotMapped]
        public bool IsOverdue => DueDate < DateTime.Now && Status != "Completed";
    }
}