using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using chessPairingSystem.Areas.Identity.Data;
using chessPairingSystem.Models;

namespace chessPairingSystem.Controllers
{
    public class MatchesController : Controller
    {
        private readonly chessPairingSystemContext _context;

        public MatchesController(chessPairingSystemContext context)
        {
            _context = context;
        }

        // GET: Matches
        // LINQ : Search matches by player name
        public async Task<IActionResult> Index(string searchString)
        {
            // LINQ : Get all matches from database
            var matches = from m in _context.Match
                          select m;

            // LINQ : Filter matches by player username if search string is provided
            if (!String.IsNullOrEmpty(searchString))
            {
                matches = matches.Where(m => m.WhitePlayer.UserName.Contains(searchString)
                                       || m.BlackPlayer.UserName.Contains(searchString));
            }

            // LINQ : Include related player data and return to view
            return View(await matches.Include(m => m.WhitePlayer)
                                     .Include(m => m.BlackPlayer)
                                     .ToListAsync());
        }

        // GET: Matches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Match
                .Include(m => m.BlackPlayer)
                .Include(m => m.WhitePlayer)
                .FirstOrDefaultAsync(m => m.GameId == id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }

        // GET: Matches/Create
        public IActionResult Create()
        {
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Matches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GameId,WhitePlayerId,BlackPlayerId,WhiteResult,BlackResult,Status,MatchDate,Location,ScheduledTime")] Match match)
        {
            if (ModelState.IsValid)
            {
                _context.Add(match);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "Id", match.BlackPlayerId);
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "Id", match.WhitePlayerId);
            return View(match);
        }

        // GET: Matches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Match.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "Id", match.BlackPlayerId);
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "Id", match.WhitePlayerId);
            return View(match);
        }

        // POST: Matches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GameId,WhitePlayerId,BlackPlayerId,WhiteResult,BlackResult,Status,MatchDate,Location,ScheduledTime")] Match match)
        {
            if (id != match.GameId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(match);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MatchExists(match.GameId))
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
            ViewData["BlackPlayerId"] = new SelectList(_context.Users, "Id", "Id", match.BlackPlayerId);
            ViewData["WhitePlayerId"] = new SelectList(_context.Users, "Id", "Id", match.WhitePlayerId);
            return View(match);
        }

        // GET: Matches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Match
                .Include(m => m.BlackPlayer)
                .Include(m => m.WhitePlayer)
                .FirstOrDefaultAsync(m => m.GameId == id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }

        // POST: Matches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var match = await _context.Match.FindAsync(id);
            if (match != null)
            {
                _context.Match.Remove(match);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MatchExists(int id)
        {
            return _context.Match.Any(e => e.GameId == id);
        }
    }
}