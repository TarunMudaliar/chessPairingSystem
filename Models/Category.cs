using System.ComponentModel.DataAnnotations;

namespace chessPairingSystem.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        // Category name is required, must be between 2 and 50 characters
        [Required]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 50 characters")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }
    }
}