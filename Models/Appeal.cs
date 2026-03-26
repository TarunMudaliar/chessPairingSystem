using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chessPairingSystem.Models
{
    public class Appeal
    {
        // ID for each appeal
        [Key]
        public int AppealId { get; set; }

        // Foreign key linking appeal to the match being disputed
        [Required]
        public int GameId { get; set; }

        // Foreign key linking appeal to the player who submitted it
        [Required]
        public string PlayerId { get; set; }

        // The appeal message describing the dispute
        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        // Current status of the appeal - Pending, Resolved, Rejected
        [StringLength(20)]
        public string? Status { get; set; }

        // Date and time the appeal was submitted
        [Required]
        public DateTime SubmittedAt { get; set; }

        // Admin's response to the appeal (nullable until admin responds)
        [StringLength(500)]
        public string? AdminResponse { get; set; }

        // Navigation property to access match details
        [ForeignKey("GameId")]
        public Match? Match { get; set; }

        // Navigation property to access player details
        [ForeignKey("PlayerId")]
        public Account? Player { get; set; }
    }
}