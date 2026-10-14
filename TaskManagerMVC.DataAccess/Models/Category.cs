using System.ComponentModel.DataAnnotations;

namespace TaskManagerMVC.DataAccess.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(50)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(7)]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color code")]
        public string ColorCode { get; set; } = "#000000";

        // Navigation Properties
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}