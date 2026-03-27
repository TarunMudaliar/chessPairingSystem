using System.ComponentModel.DataAnnotations;

namespace chessPairingSystem.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        // Category name is required and has to be under 50 characters
        [Required]
        [StringLength(50)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; }
    }
}