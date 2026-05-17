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

        public MatchesController(chessPairingSystemContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Matches
        // Admin sees all matches, Players only see their own
        [Authorize]
        public async Task<IActionResult> Index(string searchString)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // LINQ - Get all matches from database
            var matches = from m in _context.Match
                          select m;

            // LINQ - Players can only see their own matches
            if (User.IsInRole("Player"))
            {
                matches = matches.Where(m =>
                    m.WhitePlayerId == currentUser.Id ||
                    m.BlackPlayerId == currentUser.Id);
            }

            // LINQ - Filter matches by player username if search string is provided
            if (!String.IsNullOrEmpty(searchString))
            {
                matches = matches.Where(m =>
                    m.WhitePlayer.UserName.Contains(searchString) ||
                    m.BlackPlayer.UserName.Contains(searchString));
            }

            // LINQ - Include related player data, order by most recent first
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

            // Players can only view their own matches
            if (User.IsInRole("Player"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (match.WhitePlayerId != currentUser.Id && match.BlackPlayerId != currentUser.Id)
                    return Forbid();
            }

            return View(match);
        }

        // GET: Matches/SubmitResult/5
        // Shows the result submission form for a player
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

            // Make sure this player is actually in this match
            if (match.WhitePlayerId != currentUser.Id && match.BlackPlayerId != currentUser.Id)
            {
                TempData["Error"] = "You are not a player in this match.";
                return RedirectToAction(nameof(Index));
            }

            // Make sure the match is still pending
            if (match.Status != "Pending")
            {
                TempData["Error"] = "This match has already been completed or disputed.";
                return RedirectToAction(nameof(Index));
            }

            // Check if this player has already submitted a result
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
        // Core result submission - handles completion, disputes and ELO update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> SubmitResult(int id, string result)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Validate result value
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

            // Make sure this player is in this match
            if (match.WhitePlayerId != currentUser.Id && match.BlackPlayerId != currentUser.Id)
            {
                TempData["Error"] = "You are not a player in this match.";
                return RedirectToAction(nameof(Index));
            }

            // Make sure match is still pending
            if (match.Status != "Pending")
            {
                TempData["Error"] = "This match is no longer pending.";
                return RedirectToAction(nameof(Index));
            }

            bool isWhitePlayer = match.WhitePlayerId == currentUser.Id;

            // Store this player's result
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

            // Check if both players have now submitted
            if (match.WhiteResult != null && match.BlackResult != null)
            {
                // Check if results are consistent
                bool resultsMatch =
                    (match.WhiteResult == "W" && match.BlackResult == "L") ||
                    (match.WhiteResult == "L" && match.BlackResult == "W") ||
                    (match.WhiteResult == "D" && match.BlackResult == "D");

                if (resultsMatch)
                {
                    // Results agree - complete match and update ratings
                    match.Status = "Completed";
                    await UpdateRatings(match);
                    TempData["Success"] = "Match completed! Ratings have been updated.";
                }
                else
                {
                    // Results conflict - mark disputed and auto-create appeal
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
                // Only one player submitted so far
                TempData["Success"] = "Result submitted. Waiting for your opponent to submit their result.";
            }

            _context.Update(match);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ELO Rating Calculation
        // Updates both players ratings based on match result
        private async Task UpdateRatings(Match match)
        {
            var whitePlayer = await _userManager.FindByIdAsync(match.WhitePlayerId);
            var blackPlayer = await _userManager.FindByIdAsync(match.BlackPlayerId);

            if (whitePlayer == null || blackPlayer == null) return;

            int kFactor = 32;

            // Calculate expected scores using ELO formula
            double expectedWhite = 1.0 / (1.0 + Math.Pow(10, (blackPlayer.Ratings - whitePlayer.Ratings) / 400.0));
            double expectedBlack = 1.0 - expectedWhite;

            // Actual scores based on result
            double actualWhite, actualBlack;
            if (match.WhiteResult == "W") { actualWhite = 1; actualBlack = 0; }
            else if (match.WhiteResult == "L") { actualWhite = 0; actualBlack = 1; }
            else { actualWhite = 0.5; actualBlack = 0.5; }

            // Calculate and clamp new ratings between 0 and 3000
            whitePlayer.Ratings = Math.Clamp(
                (int)(whitePlayer.Ratings + kFactor * (actualWhite - expectedWhite)), 0, 3000);
            blackPlayer.Ratings = Math.Clamp(
                (int)(blackPlayer.Ratings + kFactor * (actualBlack - expectedBlack)), 0, 3000);

            await _userManager.UpdateAsync(whitePlayer);
            await _userManager.UpdateAsync(blackPlayer);
        }

        // GET: Matches/Create - Admin only
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "UserName");
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // POST: Matches/Create - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("GameId,WhitePlayerId,BlackPlayerId,WhiteResult,BlackResult,Status,MatchDate,Location,ScheduledTime")] Match match)
        {
            // Validation - prevent same player being selected for both sides
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

        // GET: Matches/Edit/5 - Admin only
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

        // POST: Matches/Edit/5 - Admin only
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("GameId,WhitePlayerId,BlackPlayerId,WhiteResult,BlackResult,Status,MatchDate,Location,ScheduledTime")] Match match)
        {
            if (id != match.GameId) return NotFound();

            // Validation - prevent same player being selected for both sides
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

        // GET: Matches/Delete/5 - Admin only
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

        // POST: Matches/Delete/5 - Admin only
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