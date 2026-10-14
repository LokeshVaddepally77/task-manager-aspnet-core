using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerMVC.DataAccess.Models
{
    public class Attachment
    {
        [Key]
        public int AttachmentId { get; set; }

        [Required(ErrorMessage = "File name is required")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [StringLength(50)]
        public string ChangeType { get; set; } = "Added";

        [DataType(DataType.DateTime)]
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Foreign Key
        [Required]
        public int TaskId { get; set; }

        // Navigation Properties
        [ForeignKey("TaskId")]
        public virtual TaskItem? Task { get; set; }
    }
}