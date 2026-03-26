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
    public class AppealsController : Controller
    {
        private readonly chessPairingSystemContext _context;

        public AppealsController(chessPairingSystemContext context)
        {
            _context = context;
        }

        // GET: Appeals
        public async Task<IActionResult> Index()
        {
            var chessPairingSystemContext = _context.Appeal.Include(a => a.Match).Include(a => a.Player);
            return View(await chessPairingSystemContext.ToListAsync());
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
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "BlackPlayerId");
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Appeals/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AppealId,GameId,PlayerId,Message,Status,SubmittedAt,AdminResponse")] Appeal appeal)
        {
            if (ModelState.IsValid)
            {
                _context.Add(appeal);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "BlackPlayerId", appeal.GameId);
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "Id", appeal.PlayerId);
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
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "BlackPlayerId", appeal.GameId);
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "Id", appeal.PlayerId);
            return View(appeal);
        }

        // POST: Appeals/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            ViewData["GameId"] = new SelectList(_context.Match, "GameId", "BlackPlayerId", appeal.GameId);
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "Id", appeal.PlayerId);
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
