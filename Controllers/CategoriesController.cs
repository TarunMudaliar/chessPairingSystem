using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using chessPairingSystem.Areas.Identity.Data;
using chessPairingSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace chessPairingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly chessPairingSystemContext _context;

        // Constructor: Connects the database context
        public CategoriesController(chessPairingSystemContext context)
        {
            _context = context;
        }

        // GET: Categories (Acts as Player Directory Dashboard)
        public async Task<IActionResult> Index()
        {
            // Pulls all registered users, linking their Category relationship details
            var players = await _context.Users
                .Include(u => u.Category)
                .OrderBy(u => u.CategoryId)
                .ThenBy(u => u.PlayerName)
                .ToListAsync();

            return View(players);
        }

        // GET: Categories/Edit/5 (Edit Player Details)
        public async Task<IActionResult> Edit(string id) // Changed type to string to match Identity GUID format
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var player = await _context.Users.FindAsync(id);
            if (player == null)
            {
                return NotFound();
            }

            // Provide hardcoded dropdown selection data items to the view
            var categories = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Year 9" },
                new SelectListItem { Value = "2", Text = "Year 10" },
                new SelectListItem { Value = "3", Text = "Year 11" },
                new SelectListItem { Value = "4", Text = "Year 12" },
                new SelectListItem { Value = "5", Text = "Year 13" }
            };

            ViewData["CategoryDropdown"] = new SelectList(categories, "Value", "Text", player.CategoryId);
            return View(player);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,PlayerName,CategoryId,Ratings,Email,UserName,NormalizedEmail,NormalizedUserName,PasswordHash,SecurityStamp,ConcurrencyStamp")] ApplicationUser updatedPlayer)
        {
            if (id != updatedPlayer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Track down user tracking properties so Identity references don't break
                    var existingPlayer = await _context.Users.FindAsync(id);
                    if (existingPlayer == null)
                    {
                        return NotFound();
                    }

                    // Update values safely
                    existingPlayer.PlayerName = updatedPlayer.PlayerName;
                    existingPlayer.CategoryId = updatedPlayer.CategoryId;
                    existingPlayer.Ratings = updatedPlayer.Ratings;

                    _context.Update(existingPlayer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlayerExists(updatedPlayer.Id))
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

            return View(updatedPlayer);
        }

        // GET: Categories/Delete/5 (Kick / Remove Player confirmation)
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var player = await _context.Users
                .Include(u => u.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (player == null)
            {
                return NotFound();
            }

            return View(player);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var player = await _context.Users.FindAsync(id);
            if (player != null)
            {
                _context.Users.Remove(player);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PlayerExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}