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
        // Only Admins can see this page to look at everyone currently waiting in the queue
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchString)
        {
            // Get all queue entries from the database
            var matchQueues = from m in _context.MatchQueue
                              select m;

            // If the admin types a name in the search box, filter the list by that name
            if (!String.IsNullOrEmpty(searchString))
            {
                matchQueues = matchQueues.Where(m => m.Player.UserName.Contains(searchString));
            }

            // Load the player names and show the list on the page
            return View(await matchQueues.Include(m => m.Player).ToListAsync());
        }

        // GET: MatchQueues/JoinQueue
        // Opens the page where a student can click to join the queue
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> JoinQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            //  Stop the player if they are already waiting in the queue
            var alreadyInQueue = await _context.MatchQueue
                .AnyAsync(q => q.PlayerId == currentUser.Id);

            if (alreadyInQueue)
            {
                TempData["Error"] = "You are already in the queue. Please wait for an opponent.";
                return RedirectToAction("MyQueue");
            }

            // Stop the player if they have an active game that isn't finished yet
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
        // This runs when the student clicks the button to find a match
        [HttpPost]
        [ValidateAntiForgeryToken] // Security step to prevent fake form submissions
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> JoinQueue(string location, string scheduledTime)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Make sure the player didn't leave the location or time fields blank
            if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(scheduledTime))
            {
                ModelState.AddModelError("", "Please select a location and scheduled time.");
                return View();
            }

            // Double check they aren't already in the queue
            var alreadyInQueue = await _context.MatchQueue
                .AnyAsync(q => q.PlayerId == currentUser.Id);

            if (alreadyInQueue)
            {
                TempData["Error"] = "You are already in the queue.";
                return RedirectToAction("MyQueue");
            }

            // Double check they don't have an unfinished match
            var activeMatch = await _context.Match
                .AnyAsync(m =>
                    (m.WhitePlayerId == currentUser.Id || m.BlackPlayerId == currentUser.Id)
                    && m.Status == "Pending");

            if (activeMatch)
            {
                TempData["Error"] = "You already have an active match.";
                return RedirectToAction("Index", "Matches");
            }

            // --- PAIRING LOGIC ---

            // Look for the first person in the queue who is NOT the current user.
            // Sort by TimeJoined so the person who has been waiting the longest gets a game first.
            var waitingPlayer = await _context.MatchQueue
                .Where(q => q.PlayerId != currentUser.Id)
                .OrderBy(q => q.TimeJoined)
                .FirstOrDefaultAsync();

            // IF an opponent is found:
            if (waitingPlayer != null)
            {
                // Use a random number generator to pick who plays White or Black
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

                // Create a new Match row with the details
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

                // Remove the opponent from the queue table because they are now playing a game
                _context.MatchQueue.Remove(waitingPlayer);

                // Save all changes to the database
                await _context.SaveChangesAsync();

                TempData["Success"] = $"You have been paired! Head to {location} at {scheduledTime}.";
                return RedirectToAction("Index", "Matches");
            }
            // IF no one is waiting:
            else
            {
                // Create a new queue entry to put the current player on hold
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
        // Shows the player their current waiting status
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> MyQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Find the player's queue record in the database
            var queueEntry = await _context.MatchQueue
                .Include(q => q.Player)
                .FirstOrDefaultAsync(q => q.PlayerId == currentUser.Id);

 
            if (queueEntry == null)
            {
                var activeMatch = await _context.Match
                    .AnyAsync(m =>
                        (m.WhitePlayerId == currentUser.Id || m.BlackPlayerId == currentUser.Id)
                        && m.Status == "Pending");

                // If a new match is found, automatically send them to their match list page
                if (activeMatch)
                {
                    TempData["Success"] = "You have been paired! Your match is ready.";
                    return RedirectToAction("Index", "Matches");
                }
            }

            return View(queueEntry);
        }

        // POST: MatchQueues/LeaveQueue
        // Deletes the player from the queue if they click the "Leave Queue" button
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> LeaveQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var queueEntry = await _context.MatchQueue
                .FirstOrDefaultAsync(q => q.PlayerId == currentUser.Id);

            // Remove them from the database so other players don't match with someone who left
            if (queueEntry != null)
            {
                _context.MatchQueue.Remove(queueEntry);
                await _context.SaveChangesAsync();
                TempData["Success"] = "You have left the queue.";
            }

            return RedirectToAction("JoinQueue");
        }

        // GET: MatchQueues/Details/5
        // Admin action to see full details of a specific queue entry
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

        // GET: MatchQueues/Delete/5
        // Admin page to confirm deleting someone from the queue manually
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

        // POST: MatchQueues/Delete/5
        // Runs the deletion when the Admin confirms the remove command
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

        // Helper check to see if a queue item exists by its ID number
        private bool MatchQueueExists(int id)
        {
            return _context.MatchQueue.Any(e => e.QueueId == id);
        }
    }
}