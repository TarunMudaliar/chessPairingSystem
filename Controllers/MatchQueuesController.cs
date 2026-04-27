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
    public class MatchQueuesController : Controller
    {
        private readonly chessPairingSystemContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MatchQueuesController(chessPairingSystemContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MatchQueues
        // Admin only - view all players currently in queue
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchString)
        {
            // LINQ - Get all queue entries from database
            var matchQueues = from m in _context.MatchQueue
                              select m;

            // LINQ - Filter queue entries by player username if search string is provided
            if (!String.IsNullOrEmpty(searchString))
            {
                matchQueues = matchQueues.Where(m => m.Player.UserName.Contains(searchString));
            }

            // LINQ - Include related player data and return to view
            return View(await matchQueues.Include(m => m.Player).ToListAsync());
        }

        // GET: MatchQueues/JoinQueue
        // Shows the join queue form for the logged in player
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> JoinQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Check if player is already in the queue
            var alreadyInQueue = await _context.MatchQueue
                .AnyAsync(q => q.PlayerId == currentUser.Id);

            if (alreadyInQueue)
            {
                TempData["Error"] = "You are already in the queue. Please wait for an opponent.";
                return RedirectToAction("MyQueue");
            }

            // Check if player already has an active (Pending) match
            var activeMatch = await _context.Match
                .AnyAsync(m =>
                    (m.WhitePlayerId == currentUser.Id || m.BlackPlayerId == currentUser.Id)
                    && m.Status == "Pending");

            if (activeMatch)
            {
                TempData["Error"] = "You already have an active match. Please complete it before joining the queue.";
                return RedirectToAction("Index", "Matches");
            }

            return View();
        }

        // POST: MatchQueues/JoinQueue
        // Core pairing logic - joins queue or pairs with waiting player
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> JoinQueue(string location, string scheduledTime)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Validate location and scheduled time are provided
            if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(scheduledTime))
            {
                ModelState.AddModelError("", "Please select a location and scheduled time.");
                return View();
            }

            // Check if player is already in the queue
            var alreadyInQueue = await _context.MatchQueue
                .AnyAsync(q => q.PlayerId == currentUser.Id);

            if (alreadyInQueue)
            {
                TempData["Error"] = "You are already in the queue.";
                return RedirectToAction("MyQueue");
            }

            // Check if player already has an active match
            var activeMatch = await _context.Match
                .AnyAsync(m =>
                    (m.WhitePlayerId == currentUser.Id || m.BlackPlayerId == currentUser.Id)
                    && m.Status == "Pending");

            if (activeMatch)
            {
                TempData["Error"] = "You already have an active match.";
                return RedirectToAction("Index", "Matches");
            }

            // PAIRING LOGIC
            // LINQ - Find the first player waiting in the queue who is not the current player
            var waitingPlayer = await _context.MatchQueue
                .Where(q => q.PlayerId != currentUser.Id)
                .OrderBy(q => q.TimeJoined) // Fairness - pair with longest waiting player first
                .FirstOrDefaultAsync();

            if (waitingPlayer != null)
            {
                // A waiting player was found - create a match

                // Randomly assign white and black pieces
                var rnd = new Random();
                string whitePlayerId, blackPlayerId;
                if (rnd.Next(2) == 0)
                {
                    whitePlayerId = currentUser.Id;
                    blackPlayerId = waitingPlayer.PlayerId;
                }
                else
                {
                    whitePlayerId = waitingPlayer.PlayerId;
                    blackPlayerId = currentUser.Id;
                }

                // Create the match record
                var match = new Match
                {
                    WhitePlayerId = whitePlayerId,
                    BlackPlayerId = blackPlayerId,
                    MatchDate = DateTime.Now,
                    Location = location,
                    ScheduledTime = scheduledTime,
                    Status = "Pending"
                };

                _context.Match.Add(match);

                // Remove the waiting player from the queue
                _context.MatchQueue.Remove(waitingPlayer);

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Match created! Head to {location} at {scheduledTime}.";
                return RedirectToAction("Index", "Matches");
            }
            else
            {
                // No waiting player found - add current player to queue
                var queueEntry = new MatchQueue
                {
                    PlayerId = currentUser.Id,
                    TimeJoined = DateTime.Now,
                    Location = location,
                    ScheduledTime = scheduledTime
                };

                _context.MatchQueue.Add(queueEntry);
                await _context.SaveChangesAsync();

                TempData["Success"] = "You have joined the queue. Waiting for an opponent...";
                return RedirectToAction("MyQueue");
            }
        }

        // GET: MatchQueues/MyQueue
        // Shows the current player their queue status
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> MyQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // LINQ - Find this player's queue entry if it exists
            var queueEntry = await _context.MatchQueue
                .Include(q => q.Player)
                .FirstOrDefaultAsync(q => q.PlayerId == currentUser.Id);

            return View(queueEntry); // null means not in queue
        }

        // POST: MatchQueues/LeaveQueue
        // Allows a player to remove themselves from the queue
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> LeaveQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // LINQ - Find and remove the player's queue entry
            var queueEntry = await _context.MatchQueue
                .FirstOrDefaultAsync(q => q.PlayerId == currentUser.Id);

            if (queueEntry != null)
            {
                _context.MatchQueue.Remove(queueEntry);
                await _context.SaveChangesAsync();
                TempData["Success"] = "You have left the queue.";
            }

            return RedirectToAction("JoinQueue");
        }

        // GET: MatchQueues/Details/5 - Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var matchQueue = await _context.MatchQueue
                .Include(m => m.Player)
                .FirstOrDefaultAsync(m => m.QueueId == id);

            if (matchQueue == null) return NotFound();

            return View(matchQueue);
        }

        // POST: MatchQueues/Delete/5 - Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var matchQueue = await _context.MatchQueue
                .Include(m => m.Player)
                .FirstOrDefaultAsync(m => m.QueueId == id);

            if (matchQueue == null) return NotFound();

            return View(matchQueue);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var matchQueue = await _context.MatchQueue.FindAsync(id);
            if (matchQueue != null)
            {
                _context.MatchQueue.Remove(matchQueue);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MatchQueueExists(int id)
        {
            return _context.MatchQueue.Any(e => e.QueueId == id);
        }
    }
}