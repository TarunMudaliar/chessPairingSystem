using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chessPairingSystem.Models
{
    public class Account : IdentityUser
    {
        // Player's display name - max 50 characters
        [StringLength(50)]
        public string? PlayerName { get; set; }

        // Foreign key linking player to their category (year level)
        public int CategoryId { get; set; }

        // Player's current rating - adjusted after each game
        public int Ratings { get; set; }

        // Navigation property to access full Category details
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}