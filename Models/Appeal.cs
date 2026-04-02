using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using chessPairingSystem.Areas.Identity.Data;

namespace chessPairingSystem.Models
{
    public class Appeal
    {
        // Unique ID for each appeal
        [Key]
        public int AppealId { get; set; }

        // Foreign key linking appeal to the match being disputed
        [Required]
        [Display(Name = "Match")]
        public int GameId { get; set; }

        // Foreign key linking appeal to the player who submitted it
        [Required]
        [Display(Name = "Player")]
        public string PlayerId { get; set; }

        // The appeal message describing the dispute - required, max 500 characters
        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        // Current status of the appeal - Pending, Resolved, Rejected - max 20 characters
        [StringLength(20)]
        public string? Status { get; set; }

        // Date and time the appeal was submitted - required
        [Required]
        [Display(Name = "Submitted At")]
        [DataType(DataType.DateTime)]
        public DateTime SubmittedAt { get; set; }

        // Admin's response to the appeal - nullable until admin responds - max 500 characters
        [StringLength(500)]
        [Display(Name = "Admin Response")]
        public string? AdminResponse { get; set; }

        // Navigation property to access match details
        [ForeignKey("GameId")]
        public Match? Match { get; set; }

        // Navigation property to access player details
        [ForeignKey("PlayerId")]
        public ApplicationUser? Player { get; set; }
    }
}