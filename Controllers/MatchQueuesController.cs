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
    public class MatchQueuesController : Controller
    {
        private readonly chessPairingSystemContext _context;

        public MatchQueuesController(chessPairingSystemContext context)
        {
            _context = context;
        }

        // GET: MatchQueues
        // LINQ - Search queue entries by player username
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
            return View(await matchQueues.Include(m => m.Player)
                                         .ToListAsync());
        }

        // GET: MatchQueues/Details/5
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

        // GET: MatchQueues/Create
        public IActionResult Create()
        {
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // POST: MatchQueues/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("QueueId,PlayerId,TimeJoined,Location,ScheduledTime")] MatchQueue matchQueue)
        {
            if (ModelState.IsValid)
            {
                _context.Add(matchQueue);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName", matchQueue.PlayerId);
            return View(matchQueue);
        }

        // GET: MatchQueues/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var matchQueue = await _context.MatchQueue.FindAsync(id);
            if (matchQueue == null)
            {
                return NotFound();
            }
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName", matchQueue.PlayerId);
            return View(matchQueue);
        }

        // POST: MatchQueues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("QueueId,PlayerId,TimeJoined,Location,ScheduledTime")] MatchQueue matchQueue)
        {
            if (id != matchQueue.QueueId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(matchQueue);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MatchQueueExists(matchQueue.QueueId))
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
            ViewData["PlayerId"] = new SelectList(_context.Users, "Id", "UserName", matchQueue.PlayerId);
            return View(matchQueue);
        }

        // GET: MatchQueues/Delete/5
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