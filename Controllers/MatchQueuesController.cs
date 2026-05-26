using chessPairingSystem.Areas.Identity.Data;
using chessPairingSystem.Models;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chessPairingSystem.Controllers
{
    public class MatchQueuesController : Controller
    {
        private readonly chessPairingSystemContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Constructor: Connects the database and the UserManager<ApplicationUser> to this controller
        public MatchQueuesController(chessPairingSystemContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: MatchQueues
        // Displays everyone waiting in the matchmaking queue, only for Admins
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchString)
        {
            // Query to fetch data from the MatchQueue table
            var matchQueues = from m in _context.MatchQueue
                              select m;

            // Filters the queue list if a student username is typed into the admin search box
            if (!String.IsNullOrEmpty(searchString))
            {
                matchQueues = matchQueues.Where(m => m.Player.UserName.Contains(searchString));
            }

            // Links student profiles to the queue records and loads the list view
            return View(await matchQueues.Include(m => m.Player).ToListAsync());
        }

        // GET: MatchQueues/JoinQueue
        // Loads the matchmaking lobby page where a student can request a new match
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> JoinQueue()
        {
            // Gets the unique profile of the logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            // Blocks the student if they are already waiting in the queue table
            var alreadyInQueue = await _context.MatchQueue
                .AnyAsync(q => q.PlayerId == currentUser.Id);

            if (alreadyInQueue)
            {
                TempData["Error"] = "You are already in the queue. Please wait for an opponent.";
                return RedirectToAction("MyQueue");
            }

            // Blocks entry if the student has an active match record marked as pending
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
        // Processes the match request, runs the automatic pairing, or places the player on hold
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> JoinQueue(string location, string scheduledTime)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Makes sure the student didn't leave the room location or time fields blank
            if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(scheduledTime))
            {
                ModelState.AddModelError("", "Please select a location and scheduled time.");
                return View();
            }

            // Double-checks they didn't reload the page to bypass the queue limits
            var alreadyInQueue = await _context.MatchQueue
                .AnyAsync(q => q.PlayerId == currentUser.Id);

            if (alreadyInQueue)
            {
                TempData["Error"] = "You are already in the queue.";
                return RedirectToAction("MyQueue");
            }

            // Double-checks they didn't bypass game limits in another browser tab
            var activeMatch = await _context.Match
                .AnyAsync(m =>
                    (m.WhitePlayerId == currentUser.Id || m.BlackPlayerId == currentUser.Id)
                    && m.Status == "Pending");

            if (activeMatch)
            {
                TempData["Error"] = "You already have an active match.";
                return RedirectToAction("Index", "Matches");
            }

            // PAIRING LOGIC: The system will attempt to find an opponent for the student in the queue database table

            // Searches for the oldest waiting queue entry belonging to a different student
            var waitingPlayer = await _context.MatchQueue
                .Where(q => q.PlayerId != currentUser.Id)
                .OrderBy(q => q.TimeJoined)
                .FirstOrDefaultAsync();

            // Outcome 1: An opponent is available in the queue database table
            if (waitingPlayer != null)
            {
                // Randomizes player colour using a number generator to ensure fairness
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

                // Creates a brand new match row inside the database using the shared pairing details
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

                // Deletes the matched opponent from the waiting list since they now have an active game
                _context.MatchQueue.Remove(waitingPlayer);

                // Saves all changes together to ensure data integrity
                await _context.SaveChangesAsync();

                TempData["Success"] = $"You have been paired! Head to {location} at {scheduledTime}.";
                return RedirectToAction("Index", "Matches");
            }
            // Outcome 2: No other students are waiting, so this player must wait on hold
            else
            {
                // Creates a new queue record tracking when and where this user wants to play
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
        // Creates the holding screen showing the player their current real-time waiting status
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> MyQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // Looks up the student's current queue entry row
            var queueEntry = await _context.MatchQueue
                .Include(q => q.Player)
                .FirstOrDefaultAsync(q => q.PlayerId == currentUser.Id);

            // If their queue row vanished, it means another player paired with them in the background
            if (queueEntry == null)
            {
                var activeMatch = await _context.Match
                    .AnyAsync(m =>
                        (m.WhitePlayerId == currentUser.Id || m.BlackPlayerId == currentUser.Id)
                        && m.Status == "Pending");

                // If the system finds a new match for them, automatically redirect them to the matches page.
                if (activeMatch)
                {
                    TempData["Success"] = "You have been paired! Your match is ready.";
                    return RedirectToAction("Index", "Matches");
                }
            }

            return View(queueEntry);
        }

        // POST: MatchQueues/LeaveQueue
        // Cancels a match request and clears the player's account row from the queue table
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Player")]
        public async Task<IActionResult> LeaveQueue()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var queueEntry = await _context.MatchQueue
                .FirstOrDefaultAsync(q => q.PlayerId == currentUser.Id);

            // Drops the database row so other users cannot accidentally match with an inactive player
            if (queueEntry != null)
            {
                _context.MatchQueue.Remove(queueEntry);
                await _context.SaveChangesAsync();
                TempData["Success"] = "You have left the queue.";
            }

            return RedirectToAction("JoinQueue");
        }

        // GET: MatchQueues/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var matchQueue = await _context.MatchQueue
                .Include(m => m.Player)
                .FirstOrDefaultAsync(m => m.QueueId == id);
            if (matchQueue == null)
            {
                return NotFound();
            }

            return View(matchQueue);
        }

        // GET: MatchQueues/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var matchQueue = await _context.MatchQueue
                .Include(m => m.Player)
                .FirstOrDefaultAsync(m => m.QueueId == id);
            if (matchQueue == null)
            {
                return NotFound();
            }

            return View(matchQueue);
        }

        // POST: MatchQueues/Delete/5
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