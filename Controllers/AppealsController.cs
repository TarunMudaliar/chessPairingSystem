using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using chessPairingSystem.Areas.Identity.Data;
using chessPairingSystem.Models;
using System.Security.Claims;

namespace chessPairingSystem.Controllers
{
    public class AppealsController : Controller
    {
        private readonly chessPairingSystemContext _context;

        // Constructor: Connects the database to this controller so it can be used
        public AppealsController(chessPairingSystemContext context)
        {
            _context = context;
        }

        // GET: Appeals/MyAppeals
        // Displays a webpage showing only the appeals filed by the logged-in student
        public async Task<IActionResult> MyAppeals()
        {
            // Gets the unique ID of the user who is currently logged in
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If they aren't logged in, send them to the login screen
            if (userId == null)
            {
                return Challenge();
            }

            // Look at the Appeals table and find records where the PlayerId matches this user
            // link the Match and Player tables to display real names and dates on the screen
            var playerAppeals = _context.Appeal
                .Where(a => a.PlayerId == userId)
                .Include(a => a.Match)
                .Include(a => a.Player);

            return View(await playerAppeals.ToListAsync());
        }

        // GET: Appeals
        // Admin View: Shows the teacher/admin a list of all complaints, with a search bar to filter by status
        public async Task<IActionResult> Index(string searchString)
        {
            // Start by getting everything in the Appeal table
            var appeals = from a in _context.Appeal
                          select a;

            // Filter the list by the appeal typed in the search bar (e.g. "Pending", "Resolved", "Rejected")
            if (!String.IsNullOrEmpty(searchString))
            {
                appeals = appeals.Where(a => a.Status.Contains(searchString));
            }

            // Run the query and link the player and match details before loading the webpage
            return View(await appeals.Include(a => a.Match)
                                     .Include(a => a.Player)
                                     .ToListAsync());
        }

        // GET: Appeals/Details/5
        // Loads the details page for a specific appeal record
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
        // Loads the blank form page so a student can type out a new appeal
        public IActionResult Create()
        {
            // Create drop-down menus for the form using existing Game IDs and Usernames from the database
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "GameId");
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // POST: Appeals/Create
        // Saves the submitted form data into the database when the student clicks submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppealId,GameId,Message")] Appeal appeal)
        {
            // Automatically attach the logged-in student's ID on the server side so they can't pretend to be someone else
            appeal.PlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Set the default startup values for a brand new appeal
            appeal.Status = "Pending";
            appeal.SubmittedAt = DateTime.Now;
            appeal.AdminResponse = "";

            // Tell the form checker to ignore fields that the server is filling out automatically
            ModelState.Remove("PlayerId");
            ModelState.Remove("Status");

            // If the form was filled out correctly, save it to the database
            if (ModelState.IsValid)
            {
                _context.Add(appeal);
                await _context.SaveChangesAsync();

                // Send the student back to their personal list to see their submitted appeal
                return RedirectToAction(nameof(MyAppeals));
            }

            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "GameId", appeal.GameId);
            return View(appeal);
        }

        // GET: Appeals/Edit/5
        // Loads the edit form page so the teacher/admin can view a appeal and type a reply
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
        // Saves the reply and the status updates made by the teacher
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppealId,GameId,PlayerId,Message,Status,SubmittedAt,AdminResponse")] Appeal appeal)
        {
            //  Makes sure the ID in the URL matches the ID of the appeal being saved
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
                    // Checks if the appeal was deleted by someone else while you were editing it
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
        // Loads a confirmation page before permanently deleting an appeal record
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
        // Deletes the appeal from the database once confirmation is submitted
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

        //  Check to see if a specific appeal ID actually exists in the database
        private bool AppealExists(int id)
        {
            return _context.Appeal.Any(e => e.AppealId == id);
        }
    }
}