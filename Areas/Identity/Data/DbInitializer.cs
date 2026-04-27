using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace chessPairingSystem.Areas.Identity.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var context = provider.GetRequiredService<chessPairingSystemContext>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

            try { context.Database.Migrate(); }
            catch (Exception ex) { Console.WriteLine("Migration failed: " + ex.Message); }

            //  Roles 
            foreach (var role in new[] { "Admin", "Player" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            //  Categories 
            if (!context.Category.Any())
            {
                foreach (var name in new[] { "Year 9", "Year 10", "Year 11", "Year 12", "Year 13" })
                    context.Category.Add(new chessPairingSystem.Models.Category { CategoryName = name });
                context.SaveChanges();
            }

            //  Admin user 
            const string adminEmail = "admin@chess.com";
            const string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    PlayerName = "Admin",
                    Ratings = 0
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
                else
                    Console.WriteLine("Failed to create admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // players 
            var rnd = new Random(12345);
            var categories = context.Category.ToList();

            if (!context.Users.Any(u => u.Email != adminEmail))
            {
                var demoNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy" };
                foreach (var name in demoNames)
                {
                    var email = name.ToLower() + "@example.com";
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        PlayerName = name + " " + (char)('A' + rnd.Next(0, 26)),
                        CategoryId = categories[rnd.Next(categories.Count)].CategoryId,
                        Ratings = rnd.Next(800, 2200)
                    };
                    var result = await userManager.CreateAsync(user, "Password123!");
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, "Player");
                    else
                        Console.WriteLine("Failed to create " + email + ": " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            //  Matches 
            if (!context.Match.Any())
            {
                var users = context.Users.Where(u => u.Email != adminEmail).ToList();
                for (int i = 0; i < 20; i++)
                {
                    var white = users[rnd.Next(users.Count)];
                    var black = users[(users.IndexOf(white) + 1 + rnd.Next(users.Count - 1)) % users.Count];
                    var match = new chessPairingSystem.Models.Match
                    {
                        WhitePlayerId = white.Id,
                        BlackPlayerId = black.Id,
                        MatchDate = DateTime.Now.AddDays(-rnd.Next(0, 30)).AddHours(-rnd.Next(0, 72)),
                        Location = "Chess Club",
                        ScheduledTime = "Lunchtime",
                        Status = rnd.NextDouble() > 0.4 ? "Completed" : "Pending"
                    };
                    if (match.Status == "Completed")
                    {
                        var outcome = rnd.Next(3);
                        if (outcome == 0) { match.WhiteResult = "W"; match.BlackResult = "L"; }
                        else if (outcome == 1) { match.WhiteResult = "L"; match.BlackResult = "W"; }
                        else { match.WhiteResult = "D"; match.BlackResult = "D"; }
                    }
                    context.Match.Add(match);
                }
                context.SaveChanges();
            }

            //  Appeals
            if (!context.Appeal.Any())
            {
                var matches = context.Match.ToList();
                var users = context.Users.Where(u => u.Email != adminEmail).ToList();
                for (int i = 0; i < Math.Min(10, matches.Count); i++)
                {
                    context.Appeal.Add(new chessPairingSystem.Models.Appeal
                    {
                        GameId = matches[rnd.Next(matches.Count)].GameId,
                        PlayerId = users[rnd.Next(users.Count)].Id,
                        Message = "I would like to appeal the result because...",
                        Status = rnd.NextDouble() > 0.6 ? "Resolved" : "Pending",
                        SubmittedAt = DateTime.Now.AddDays(-rnd.Next(0, 10)),
                        AdminResponse = rnd.NextDouble() > 0.6 ? "Reviewed and resolved." : null
                    });
                }
                context.SaveChanges();
            }

            //  Match Queue 
            if (!context.MatchQueue.Any())
            {
                var users = context.Users.Where(u => u.Email != adminEmail).ToList();
                for (int i = 0; i < Math.Min(3, users.Count); i++)
                {
                    context.MatchQueue.Add(new chessPairingSystem.Models.MatchQueue
                    {
                        PlayerId = users[i].Id,
                        TimeJoined = DateTime.Now.AddMinutes(-rnd.Next(0, 300)),
                        Location = "Chess Club",
                        ScheduledTime = "After School"
                    });
                }
                context.SaveChanges();
            }
        }
    }
}