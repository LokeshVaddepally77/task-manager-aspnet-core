using System.ComponentModel.DataAnnotations;

namespace TaskManagerMVC.DataAccess.Models
{
    public class Tag
    {
        [Key]
        public int TagId { get; set; }

        [Required(ErrorMessage = "Tag name is required")]
        [StringLength(30)]
        public string Name { get; set; } = string.Empty;

        [StringLength(7)]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color code")]
        public string ColorCode { get; set; } = "#000000";

        // Navigation Properties
        public virtual ICollection<TaskTag> TaskTags { get; set; } = new List<TaskTag>();
    }
}