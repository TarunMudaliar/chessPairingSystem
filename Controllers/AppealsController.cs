using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using chessPairingSystem.Areas.Identity.Data;
using chessPairingSystem.Models;
using System.Security.Claims; // Required to extract the logged-in User's ID

namespace chessPairingSystem.Controllers
{
    public class AppealsController : Controller
    {
        private readonly chessPairingSystemContext _context;

        public AppealsController(chessPairingSystemContext context)
        {
            _context = context;
        }

        // GET: Appeals/MyAppeals
        // Fetches appeals filed only by the currently logged-in player
        public async Task<IActionResult> MyAppeals()
        {
            // 1. Get the unique User ID of the currently logged-in player
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Challenge(); // Forces them to log in if their session expired
            }

            // 2. Query the database, filtering appeals where PlayerId matches the current user
            var playerAppeals = _context.Appeal
                .Where(a => a.PlayerId == userId)
                .Include(a => a.Match)
                .Include(a => a.Player);

            // 3. Send the filtered list to the view
            return View(await playerAppeals.ToListAsync());
        }

        // GET: Appeals
        // LINQ - Search appeals by status (Admin view)
        public async Task<IActionResult> Index(string searchString)
        {
            // LINQ - Get all appeals from database
            var appeals = from a in _context.Appeal
                          select a;

            // LINQ - Filter appeals by status if search string is provided
            if (!String.IsNullOrEmpty(searchString))
            {
                appeals = appeals.Where(a => a.Status.Contains(searchString));
            }

            // LINQ - Include related match and player data and return to view
            return View(await appeals.Include(a => a.Match)
                                     .Include(a => a.Player)
                                     .ToListAsync());
        }

        // GET: Appeals/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appeal = await _context.Appeal
                .Include(a => a.Match)
                .Include(a => a.Player)
                .FirstOrDefaultAsync(m => m.AppealId == id);
            if (appeal == null)
            {
                return NotFound();
            }

            return View(appeal);
        }

        // GET: Appeals/Create
        public IActionResult Create()
        {
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "GameId");
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // POST: Appeals/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppealId,GameId,Message")] Appeal appeal)
        {
            // 1. Automatically attach the logged-in Player's ID to prevent identity spoofing
            appeal.PlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Force default values on the server side for safety and automation
            appeal.Status = "Pending";
            appeal.SubmittedAt = DateTime.Now;
            appeal.AdminResponse = "";

            // Clear manual injection properties out of validation states
            ModelState.Remove("PlayerId");
            ModelState.Remove("Status");

            if (ModelState.IsValid)
            {
                _context.Add(appeal);
                await _context.SaveChangesAsync();

                // 3. Redirect back to the player's dashboard layout
                return RedirectToAction(nameof(MyAppeals));
            }

            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "GameId", appeal.GameId);
            return View(appeal);
        }

        // GET: Appeals/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appeal = await _context.Appeal.FindAsync(id);
            if (appeal == null)
            {
                return NotFound();
            }
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "GameId", appeal.GameId);
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName", appeal.PlayerId);
            return View(appeal);
        }

        // POST: Appeals/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppealId,GameId,PlayerId,Message,Status,SubmittedAt,AdminResponse")] Appeal appeal)
        {
            if (id != appeal.AppealId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appeal);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppealExists(appeal.AppealId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "GameId", appeal.GameId);
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName", appeal.PlayerId);
            return View(appeal);
        }

        // GET: Appeals/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appeal = await _context.Appeal
                .Include(a => a.Match)
                .Include(a => a.Player)
                .FirstOrDefaultAsync(m => m.AppealId == id);
            if (appeal == null)
            {
                return NotFound();
            }

            return View(appeal);
        }

        // POST: Appeals/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appeal = await _context.Appeal.FindAsync(id);
            if (appeal != null)
            {
                _context.Appeal.Remove(appeal);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppealExists(int id)
        {
            return _context.Appeal.Any(e => e.AppealId == id);
        }
    }
}