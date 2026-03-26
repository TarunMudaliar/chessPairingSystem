using chessPairingSystem.Models;
using chessPairingSystem.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace chessPairingSystem.Areas.Identity.Data;

public class chessPairingSystemContext : IdentityDbContext<ApplicationUser>
{
    public chessPairingSystemContext(DbContextOptions<chessPairingSystemContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }

public DbSet<chessPairingSystem.Models.Category> Category { get; set; } = default!;

public DbSet<chessPairingSystem.Models.Match> Match { get; set; } = default!;

public DbSet<chessPairingSystem.Models.MatchQueue> MatchQueue { get; set; } = default!;

public DbSet<chessPairingSystem.Models.Appeal> Appeal { get; set; } = default!;
}
