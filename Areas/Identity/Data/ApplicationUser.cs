using chessPairingSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chessPairingSystem.Areas.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {
        // Player's display name
        [StringLength(50)]
        [Display(Name = "Player Name")]
        public string? PlayerName { get; set; }

        // Foreign key linking player to their category e.g. Year 9, Year 10
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        // Player's current rating - automatically adjusted after each game
        public int Ratings { get; set; }

        // Navigation property to access full category details
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}