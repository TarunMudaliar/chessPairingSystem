using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chessPairingSystem.Models
{
    public class MatchQueue
    {
        // Unique ID for each queue entry
        [Key]
        public int QueueId { get; set; }

        // Foreign key linking to the player waiting in queue
        [Required]
        public string PlayerId { get; set; }

        // Date and time the player joined the queue
        [Required]
        public DateTime TimeJoined { get; set; }

        // Location where the player wants to play e.g. Chess Club
        [StringLength(20)]
        public string? Location { get; set; }

        // Scheduled time the player wants to play e.g. Lunchtime
        [StringLength(20)]
        public string? ScheduledTime { get; set; }

        // Navigation property to access player details
        [ForeignKey("PlayerId")]
        public Account? Player { get; set; }
    }
}