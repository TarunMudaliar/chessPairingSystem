using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace chessPairingSystem.Areas.Identity.Data
{
    public static class DbInitializer
    {
        
        public static void Initialize(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var context = provider.GetRequiredService<chessPairingSystemContext>();

            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database migration failed: " + ex.Message);
            }

            var rnd = new Random(12345);

            // Categories
            if (!context.Category.Any())
            {
                var catNames = new[] { "Year 9", "Year 10", "Year 11", "Year 12", "Year 13" };
                foreach (var name in catNames)
                {
                    context.Category.Add(new chessPairingSystem.Models.Category { CategoryName = name });
                }
                context.SaveChanges();
            }

            // Users
            var userManager = provider.GetService<UserManager<ApplicationUser>>();
            if (userManager != null && !context.Users.Any())
            {
                var categoriesList = context.Category.ToList();
                var demoNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Judy" };
                foreach (var name in demoNames)
                {
                    var email = name.ToLower() + "@example.com";
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        PlayerName = name + " " + (new string((char)('A' + rnd.Next(0, 26)), 1)),
                        CategoryId = categoriesList[rnd.Next(categoriesList.Count)].CategoryId,
                        Ratings = rnd.Next(800, 2200)
                    };
                    var pwd = "Password123!";
                    var res = userManager.CreateAsync(user, pwd).GetAwaiter().GetResult();
                    if (!res.Succeeded)
                    {
                        Console.WriteLine("Failed to create demo user " + email + ": " + string.Join(", ", res.Errors.Select(e => e.Description)));
                    }
                }
            }

            context.SaveChanges();

            // Matches
            if (!context.Match.Any())
            {
                var users = context.Users.ToList();
                for (int i = 0; i < 20; i++)
                {
                    var white = users[rnd.Next(users.Count)];
                    var black = users[rnd.Next(users.Count)];
                    if (white.Id == black.Id)
                    {
                        // ensure different players
                        black = users[(users.IndexOf(white) + 1) % users.Count];
                    }
                    var match = new chessPairingSystem.Models.Match
                    {
                        WhitePlayerId = white.Id,
                        BlackPlayerId = black.Id,
                        MatchDate = DateTime.Now.AddDays(-rnd.Next(0, 30)).AddHours(-rnd.Next(0, 72)),
                        Location = "Chess Club",
                        ScheduledTime = "Lunchtime",
                        Status = (rnd.NextDouble() > 0.4) ? "Completed" : "Pending",
                    };
                    if (match.Status == "Completed")
                    {
                        var outcome = rnd.Next(0, 3);
                        if (outcome == 0) { match.WhiteResult = "W"; match.BlackResult = "L"; }
                        else if (outcome == 1) { match.WhiteResult = "L"; match.BlackResult = "W"; }
                        else { match.WhiteResult = "D"; match.BlackResult = "D"; }
                    }
                    context.Match.Add(match);
                }
                context.SaveChanges();
            }

            // Appeals
            if (!context.Appeal.Any())
            {
                var matches = context.Match.ToList();
                var users = context.Users.ToList();
                for (int i = 0; i < Math.Min(10, matches.Count); i++)
                {
                    var m = matches[rnd.Next(matches.Count)];
                    var player = users[rnd.Next(users.Count)];
                    var appeal = new chessPairingSystem.Models.Appeal
                    {
                        GameId = m.GameId,
                        PlayerId = player.Id,
                        Message = "I would like to appeal the result because...",
                        Status = (rnd.NextDouble() > 0.6) ? "Resolved" : "Pending",
                        SubmittedAt = DateTime.Now.AddDays(-rnd.Next(0, 10)),
                        AdminResponse = (rnd.NextDouble() > 0.6) ? "Reviewed and resolved." : null
                    };
                    context.Appeal.Add(appeal);
                }
                context.SaveChanges();
            }

            // MatchQueue
            if (!context.MatchQueue.Any())
            {
                var users = context.Users.ToList();
                for (int i = 0; i < Math.Min(10, users.Count); i++)
                {
                    var u = users[i];
                    var q = new chessPairingSystem.Models.MatchQueue
                    {
                        PlayerId = u.Id,
                        TimeJoined = DateTime.Now.AddMinutes(-rnd.Next(0, 300)),
                        Location = "Chess Club",
                        ScheduledTime = "After School"
                    };
                    context.MatchQueue.Add(q);
                }
                context.SaveChanges();
            }
        }
    }
}
