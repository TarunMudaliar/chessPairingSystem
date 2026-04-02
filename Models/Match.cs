using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using chessPairingSystem.Areas.Identity.Data;

namespace chessPairingSystem.Models
{
    public class Match
    {
        [Key]
        public int GameId { get; set; }

        // Foreign key for the white pieces player
        [Required]
        public string WhitePlayerId { get; set; }

        // Foreign key for the black pieces player
        [Required]
        public string BlackPlayerId { get; set; }

        // Result entered by white player - W, L or D - max 10 characters
        [StringLength(10)]
        [Display(Name = "White Result")]
        public string? WhiteResult { get; set; }

        // Result entered by black player - W, L or D - max 10 characters
        [StringLength(10)]
        [Display(Name = "Black Result")]
        public string? BlackResult { get; set; }

        // Match status - Pending, Completed or Disputed - max 10 characters
        [StringLength(10)]
        public string? Status { get; set; }

        // Date of the match - cannot be in the past
        [Required]
        [Display(Name = "Match Date")]
        [DataType(DataType.Date)]
        public DateTime MatchDate { get; set; }

        // Location where the game will be played e.g. Chess Club - max 20 characters
        [StringLength(20)]
        public string? Location { get; set; }

        // Scheduled time for the game e.g. Lunchtime - max 20 characters
        [StringLength(20)]
        [Display(Name = "Scheduled Time")]
        public string? ScheduledTime { get; set; }

        // Navigation property for white player
        [ForeignKey("WhitePlayerId")]
        public ApplicationUser? WhitePlayer { get; set; }

        // Navigation property for black player
        [ForeignKey("BlackPlayerId")]
        public ApplicationUser? BlackPlayer { get; set; }

        // Navigation property for appeals linked to this match
        public ICollection<Appeal>? Appeals { get; set; }
    }
}