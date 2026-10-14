using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerMVC.DataAccess.Models
{
    public class TaskHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [StringLength(500)]
        public string OldValue { get; set; } = string.Empty;

        [StringLength(500)]
        public string NewValue { get; set; } = string.Empty;

        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime ChangedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string ChangedBy { get; set; } = string.Empty;

        // Foreign Key
        [Required]
        public int TaskId { get; set; }

        // Navigation Properties
        [ForeignKey("TaskId")]
        public virtual TaskItem? Task { get; set; }
    }
}