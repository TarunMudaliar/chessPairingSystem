using chessPairingSystem.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using chessPairingSystem.Areas.Identity.Data;

namespace chessPairingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly chessPairingSystemContext _context;

        // Constructor: Connects the database to this controller to grab student data
        public HomeController(chessPairingSystemContext context)
        {
            _context = context;
        }

        // GET: / (Home Page)
        // Loads the main welcome page of the web application
        public IActionResult Index()
        {
            return View();
        }

        // GET: Home/Standings (Leaderboard Page)
        // Pulls all registered users, sorts them by skill, and shows them in the standings screen
        public async Task<IActionResult> Standings()
        {
            // 1. Look at the Users table
            // 2. Link the school year levels from the Category table to see them on the page
            // 3. Sort players by Elo rating so the highest score is at the top
            // 4. Convert it into a clean list asynchronously to keep the website responsive
            var sortedPlayers = await _context.Users
                .Include(u => u.Category)
                .OrderByDescending(u => u.Ratings)
                .ToListAsync();

            // Pass the sorted list over to the Standings webpage view to display it
            return View(sortedPlayers);
        }

        // Error Page Handler
        // If the web app crashes, this catches the error, stops the browser from caching the broken page, 
        // and generates a unique Request ID code to help with debugging the bug.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}