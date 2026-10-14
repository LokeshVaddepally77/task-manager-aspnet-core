using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerMVC.DataAccess.Models
{
    public class TaskTag
    {
        // Composite Key configured in DbContext
        public int TaskId { get; set; }
        public int TagId { get; set; }

        // Navigation Properties
        [ForeignKey("TaskId")]
        public virtual TaskItem? Task { get; set; }

        [ForeignKey("TagId")]
        public virtual Tag? Tag { get; set; }
    }
}