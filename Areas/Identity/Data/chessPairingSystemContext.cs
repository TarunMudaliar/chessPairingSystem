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

        builder.Entity<Match>()
            .HasOne(m => m.WhitePlayer)
            .WithMany()
            .HasForeignKey(m => m.WhitePlayerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Match>()
            .HasOne(m => m.BlackPlayer)
            .WithMany()
            .HasForeignKey(m => m.BlackPlayerId)
            .OnDelete(DeleteBehavior.NoAction);
    }

    public DbSet<Category> Category { get; set; } = default!;
    public DbSet<Match> Match { get; set; } = default!;
    public DbSet<MatchQueue> MatchQueue { get; set; } = default!;
    public DbSet<Appeal> Appeal { get; set; } = default!;
}
