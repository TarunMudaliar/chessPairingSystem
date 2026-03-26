using chessPairingSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chessPairingSystem.Areas.Identity.Data
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(50)]
        public string? PlayerName { get; set; }

        public int CategoryId { get; set; }

        public int Ratings { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}