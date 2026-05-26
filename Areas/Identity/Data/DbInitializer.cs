using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace chessPairingSystem.Areas.Identity.Data
{
    public static class DbInitializer
    {
        // Main setup function that runs when the application starts up
        public static async Task Initialize(IServiceProvider services)
        {
            // Set up a temporary workspace to safely fetch our database tools
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            // Get the database context and the user/role security managers
            var context = provider.GetRequiredService<chessPairingSystemContext>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

            // Automatically build or update the SQL database tables so you don't have to do it manually
            try { context.Database.Migrate(); }
            catch (Exception ex) { Console.WriteLine("Database setup failed: " + ex.Message); }

            // USER ROLES SETUP
            // Create the "Admin" and "Player" groups if they don't exist yet
            foreach (var role in new[] { "Admin", "Player" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // CHESS CATEGORIES SETUP
            // If the category list is empty, add the school year levels
            if (!context.Category.Any())
            {
                foreach (var name in new[] { "Year 9", "Year 10", "Year 11", "Year 12", "Year 13" })
                    context.Category.Add(new chessPairingSystem.Models.Category { CategoryName = name });

                context.SaveChanges(); // Save changes to the database to generate their ID numbers
            }

            // --- ADMIN USER SETUP ---
            const string adminEmail = "admin@chess.com";
            const string adminPassword = "Admin@123";

            // Creates the admin account if it doesn't exist.
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
                    await userManager.AddToRoleAsync(admin, "Admin"); // Give the account admin permissions
                else
                    Console.WriteLine("Failed to create admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // FAKE PLAYER DATA SETUP
            // Use a fixed random seed (12345) so it generates the exact same players every time you reset
            var rnd = new Random(12345);
            var categories = context.Category.ToList();

            // If there are no regular players in the database, create 10 demo accounts
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
                        PlayerName = name + " " + (char)('A' + rnd.Next(0, 26)), // Adds a random middle initial
                        CategoryId = categories[rnd.Next(categories.Count)].CategoryId, // Picks a random year level
                        Ratings = rnd.Next(800, 2200) // Gives them a realistic starting chess rating
                    };

                    var result = await userManager.CreateAsync(user, "Password123!");
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(user, "Player"); // Give regular player access
                    else
                        Console.WriteLine("Failed to create " + email + ": " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // FAKE DATA FOR MATCHES SETUP
            // Create 20 past games to fill in the match history screens
            if (!context.Match.Any())
            {
                var users = context.Users.Where(u => u.Email != adminEmail).ToList();
                for (int i = 0; i < 20; i++)
                {
                    var white = users[rnd.Next(users.Count)];
                    // forces the system to pick a different person for the black pieces
                    var black = users[(users.IndexOf(white) + 1 + rnd.Next(users.Count - 1)) % users.Count];

                    var match = new chessPairingSystem.Models.Match
                    {
                        WhitePlayerId = white.Id,
                        BlackPlayerId = black.Id,
                        MatchDate = DateTime.Now.AddDays(-rnd.Next(0, 30)).AddHours(-rnd.Next(0, 72)), // Gives a random past date
                        Location = "Chess Club",
                        ScheduledTime = "Lunchtime",
                        Status = rnd.NextDouble() > 0.4 ? "Completed" : "Pending" // Makes some games finished and some still waiting
                    };

                    // For the completed games, randomly decide who won or if it was a draw
                    if (match.Status == "Completed")
                    {
                        var outcome = rnd.Next(3); // 0 = White wins, 1 = Black wins, 2 = Draw
                        if (outcome == 0) { match.WhiteResult = "W"; match.BlackResult = "L"; }
                        else if (outcome == 1) { match.WhiteResult = "L"; match.BlackResult = "W"; }
                        else { match.WhiteResult = "D"; match.BlackResult = "D"; }
                    }
                    context.Match.Add(match);
                }
                context.SaveChanges();
            }

            // FAKE DATA FOR APPEALS SETUP
            // Add up to 10 fake score disputes
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
                        Status = rnd.NextDouble() > 0.6 ? "Resolved" : "Pending", // Randomly marks some as fixed and some as open
                        SubmittedAt = DateTime.Now.AddDays(-rnd.Next(0, 10)),
                        AdminResponse = rnd.NextDouble() > 0.6 ? "Reviewed and resolved." : null // Adds an admin reply to resolved ones
                    });
                }
                context.SaveChanges();
            }

            // FAKE DATA FOR WAITING QUEUE SETUP
            // Places 3 random players into the active matchmaking line so the queue isn't empty when launched
            if (!context.MatchQueue.Any())
            {
                var users = context.Users.Where(u => u.Email != adminEmail).ToList();
                for (int i = 0; i < Math.Min(3, users.Count); i++)
                {
                    context.MatchQueue.Add(new chessPairingSystem.Models.MatchQueue
                    {
                        PlayerId = users[i].Id,
                        TimeJoined = DateTime.Now.AddMinutes(-rnd.Next(0, 300)), // Mixes up how long they have been waiting
                        Location = "Chess Club",
                        ScheduledTime = "After School"
                    });
                }
                context.SaveChanges(); // Push everything to the SQL database file
            }
        }
    }
}