using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using chessPairingSystem.Areas.Identity.Data;
using chessPairingSystem.Models;

namespace chessPairingSystem.Controllers
{
    public class MatchesController : Controller
    {
        private readonly chessPairingSystemContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor: Connects the database and the UserManager<ApplicationUser> to this controller
        public MatchesController(chessPairingSystemContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Matches
        // Displays webpage listing chess games, filtered based on account roles
        [Authorize]
        public async Task<IActionResult> Index(string searchString)
        {
            // Gets the unique profile of the logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            // Set up a query to fetch data from the Match table
            var matches = from m in _context.Match
                          select m;

            // filters the list so students can only look at games they played in
            if (User.IsInRole("Player"))
            {
                matches = matches.Where(m =>
                    m.WhitePlayerId == currentUser.Id ||
                    m.BlackPlayerId == currentUser.Id);
            }

            // Filters the games list if a student username is typed into the search box
            if (!String.IsNullOrEmpty(searchString))
            {
                matches = matches.Where(m =>
                    m.WhitePlayer.UserName.Contains(searchString) ||
                    m.BlackPlayer.UserName.Contains(searchString));
            }

            // Links student profiles to the matches and sorts them by date to display the list
            return View(await matches.Include(m => m.WhitePlayer)
                                     .Include(m => m.BlackPlayer)
                                     .OrderByDescending(m => m.MatchDate)
                                     .ToListAsync());
        }

        // GET: Matches/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var match = await _context.Match
                .Include(m => m.BlackPlayer)
                .Include(m => m.WhitePlayer)
                .FirstOrDefaultAsync(m => m.GameId == id);

            if (match == null) return NotFound();

            if (User.IsInRole("Player"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (match.WhitePlayerId != currentUser.Id && match.BlackPlayerId != currentUser.Id)
                    return Forbid();
            }

            return View(match);
        }

        // GET: Matches/SubmitResult/5
        // Loads the score submission screen for the game
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> SubmitResult(int? id)
        {
            if (id == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            var match = await _context.Match
                .Include(m => m.WhitePlayer)
                .Include(m => m.BlackPlayer)
                .FirstOrDefaultAsync(m => m.GameId == id);

            if (match == null) return NotFound();

            // Stops outside accounts from submitting results for someone else's game
            if (match.WhitePlayerId != currentUser.Id && match.BlackPlayerId != currentUser.Id)
            {
                TempData["Error"] = "You are not a player in this match.";
                return RedirectToAction(nameof(Index));
            }

            // Ensures completed or disputed matches cannot have scores updated again
            if (match.Status != "Pending")
            {
                TempData["Error"] = "This match has already been completed or disputed.";
                return RedirectToAction(nameof(Index));
            }

            // Prevents a player from resubmitting a score if they have already sent one in
            bool isWhitePlayer = match.WhitePlayerId == currentUser.Id;
            if (isWhitePlayer && match.WhiteResult != null)
            {
                TempData["Error"] = "You have already submitted a result for this match.";
                return RedirectToAction(nameof(Index));
            }
            if (!isWhitePlayer && match.BlackResult != null)
            {
                TempData["Error"] = "You have already submitted a result for this match.";
                return RedirectToAction(nameof(Index));
            }

            return View(match);
        }

        // POST: Matches/SubmitResult/5
        // Processes student scores, checks for cheating or mistakes, and calculates updated rankings
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> SubmitResult(int id, string result)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Ensures input text matches valid score options
            if (result != "W" && result != "L" && result != "D")
            {
                TempData["Error"] = "Invalid result. Please select Win, Loss or Draw.";
                return RedirectToAction(nameof(SubmitResult), new { id });
            }

            var match = await _context.Match
                .Include(m => m.WhitePlayer)
                .Include(m => m.BlackPlayer)
                .FirstOrDefaultAsync(m => m.GameId == id);

            if (match == null) return NotFound();

            if (match.WhitePlayerId != currentUser.Id && match.BlackPlayerId != currentUser.Id)
            {
                TempData["Error"] = "You are not a player in this match.";
                return RedirectToAction(nameof(Index));
            }

            if (match.Status != "Pending")
            {
                TempData["Error"] = "This match is no longer pending.";
                return RedirectToAction(nameof(Index));
            }

            bool isWhitePlayer = match.WhitePlayerId == currentUser.Id;

            // Save the typed score into the correct player slot in the database record
            if (isWhitePlayer)
            {
                if (match.WhiteResult != null)
                {
                    TempData["Error"] = "You have already submitted a result.";
                    return RedirectToAction(nameof(Index));
                }
                match.WhiteResult = result;
            }
            else
            {
                if (match.BlackResult != null)
                {
                    TempData["Error"] = "You have already submitted a result.";
                    return RedirectToAction(nameof(Index));
                }
                match.BlackResult = result;
            }

            // Checks outcomes if both players have submitted their scores
            if (match.WhiteResult != null && match.BlackResult != null)
            {
                // Verify that both score submissions match up
                bool resultsMatch =
                    (match.WhiteResult == "W" && match.BlackResult == "L") ||
                    (match.WhiteResult == "L" && match.BlackResult == "W") ||
                    (match.WhiteResult == "D" && match.BlackResult == "D");

                if (resultsMatch)
                {
                    // Closes the game record and runs the ELO calculation formula
                    match.Status = "Completed";
                    await UpdateRatings(match);
                    TempData["Success"] = "Match completed! Ratings have been updated.";
                }
                else
                {
                    // Flags the game record and generates an appeal automatically
                    match.Status = "Disputed";

                    var appeal = new Appeal
                    {
                        GameId = match.GameId,
                        PlayerId = currentUser.Id,
                        Message = $"Conflicting results submitted. White player submitted '{match.WhiteResult}', Black player submitted '{match.BlackResult}'. Admin review required.",
                        Status = "Pending",
                        SubmittedAt = DateTime.Now
                    };

                    _context.Appeal.Add(appeal);
                    TempData["Success"] = "Result submitted. There is a conflict — an appeal has been automatically raised for admin review.";
                }
            }
            else
            {
                // Keeps the game open if the opponent has not sent a score yet
                TempData["Success"] = "Result submitted. Waiting for your opponent to submit their result.";
            }

            _context.Update(match);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Elo Rating Calculation Method
        // Updates student rating mathematically when a match finishes
        private async Task UpdateRatings(Match match)
        {
            var whitePlayer = await _userManager.FindByIdAsync(match.WhitePlayerId);
            var blackPlayer = await _userManager.FindByIdAsync(match.BlackPlayerId);

            if (whitePlayer == null || blackPlayer == null) return;

            int kFactor = 32; // Maximum rating that can shift in a single game

            // Standard Elo formula calculation based on current rating to find expected outcomes
            double expectedWhite = 1.0 / (1.0 + Math.Pow(10, (blackPlayer.Ratings - whitePlayer.Ratings) / 400.0));
            double expectedBlack = 1.0 - expectedWhite;

            // Assign numerical values to outcome states
            double actualWhite, actualBlack;
            if (match.WhiteResult == "W") { actualWhite = 1; actualBlack = 0; }
            else if (match.WhiteResult == "L") { actualWhite = 0; actualBlack = 1; }
            else { actualWhite = 0.5; actualBlack = 0.5; }

            // Adjust ratings and confirm values
            whitePlayer.Ratings = Math.Clamp(
                (int)(whitePlayer.Ratings + kFactor * (actualWhite - expectedWhite)), 0, 3000);
            blackPlayer.Ratings = Math.Clamp(
                (int)(blackPlayer.Ratings + kFactor * (actualBlack - expectedBlack)), 0, 3000);

            await _userManager.UpdateAsync(whitePlayer);
            await _userManager.UpdateAsync(blackPlayer);
        }

        // GET: Matches/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "UserName");
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // POST: Matches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("GameId,WhitePlayerId,BlackPlayerId,WhiteResult,BlackResult,Status,MatchDate,Location,ScheduledTime")] Match match)
        {
            if (match.WhitePlayerId == match.BlackPlayerId)
                ModelState.AddModelError(string.Empty, "White Player and Black Player cannot be the same player.");

            if (ModelState.IsValid)
            {
                _context.Add(match);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "UserName", match.BlackPlayerId);
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "UserName", match.WhitePlayerId);
            return View(match);
        }

        // GET: Matches/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var match = await _context.Match.FindAsync(id);
            if (match == null) return NotFound();

            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "UserName", match.BlackPlayerId);
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "UserName", match.WhitePlayerId);
            return View(match);
        }

        // POST: Matches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("GameId,WhitePlayerId,BlackPlayerId,WhiteResult,BlackResult,Status,MatchDate,Location,ScheduledTime")] Match match)
        {
            if (id != match.GameId) return NotFound();

            if (match.WhitePlayerId == match.BlackPlayerId)
                ModelState.AddModelError(string.Empty, "White Player and Black Player cannot be the same player.");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(match);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MatchExists(match.GameId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "UserName", match.BlackPlayerId);
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "UserName", match.WhitePlayerId);
            return View(match);
        }

        // GET: Matches/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var match = await _context.Match
                .Include(m => m.BlackPlayer)
                .Include(m => m.WhitePlayer)
                .FirstOrDefaultAsync(m => m.GameId == id);

            if (match == null) return NotFound();
            return View(match);
        }

        // POST: Matches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var match = await _context.Match.FindAsync(id);
            if (match != null)
                _context.Match.Remove(match);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MatchExists(int id)
        {
            return _context.Match.Any(e => e.GameId == id);
        }
    }
}