using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Result entered by white player - W, L or D
        [StringLength(10)]
        public string? WhiteResult { get; set; }

        // Result entered by black player - W, L or D
        [StringLength(10)]
        public string? BlackResult { get; set; }

        // Match status - e.g. Pending, Completed
        [StringLength(10)]
        public string? Status { get; set; }

        // Date and time the match was created
        [Required]
        public DateTime MatchDate { get; set; }

        // Location where the game will be played e.g. Chess Club
        [StringLength(20)]
        public string? Location { get; set; }

        // Scheduled time for the game e.g. Lunchtime
        [StringLength(20)]
        public string? ScheduledTime { get; set; }

        // Navigation property for white player
        [ForeignKey("WhitePlayerId")]
        public Account? WhitePlayer { get; set; }

        // Navigation property for black player
        [ForeignKey("BlackPlayerId")]
        public Account? BlackPlayer { get; set; }

        // Navigation property for appeals linked to this match
        public ICollection<Appeal>? Appeals { get; set; }
    }
}