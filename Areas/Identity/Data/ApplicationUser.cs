using chessPairingSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chessPairingSystem.Areas.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {
        // Player's display name - max 50 characters
        [StringLength(50)]
        [Display(Name = "Player Name")]
        public string? PlayerName { get; set; }

        // Foreign key linking player to their category e.g. Year 9, Year 10
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        // Player's current rating - automatically adjusted after each game - must be between 0 and 9999
        [Range(0, 3000, ErrorMessage = "Rating must be between 0 and 3000")]
        public int Ratings { get; set; }

        // Navigation property to access full category details
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}