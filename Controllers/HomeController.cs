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

        // Inject the database context to get player ratings
        public HomeController(chessPairingSystemContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Home/Standings
        public async Task<IActionResult> Standings()
        {
            // Fetch users from the database and include their Category data (Year Level)
            // Order them by Ratings descending so the highest rating is #1
            var sortedPlayers = await _context.Users
                .Include(u => u.Category)
                .OrderByDescending(u => u.Ratings)
                .ToListAsync();

            return View(sortedPlayers);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}