using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Models;

namespace HakemYorumlari.Controllers
{
    public class HakemYorumuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HakemYorumuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: HakemYorumu
        public async Task<IActionResult> Index()
        {
            var yorumlar = await _context.HakemYorumlari
                .Include(h => h.Pozisyon)
                .ThenInclude(p => p.Mac)
                .OrderByDescending(h => h.YorumTarihi)
                .ToListAsync();
            return View(yorumlar);
        }

        // GET: HakemYorumu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hakemYorumu = await _context.HakemYorumlari
                .Include(h => h.Pozisyon)
                .ThenInclude(p => p.Mac)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (hakemYorumu == null)
            {
                return NotFound();
            }

            return View(hakemYorumu);
        }

        // GET: HakemYorumu/Create
        public IActionResult Create(int? pozisyonId)
        {
            if (pozisyonId.HasValue)
            {
                ViewBag.PozisyonId = pozisyonId.Value;
                var pozisyon = _context.Pozisyonlar
                    .Include(p => p.Mac)
                    .FirstOrDefault(p => p.Id == pozisyonId.Value);
                ViewBag.Pozisyon = pozisyon;
            }
            return View();
        }

        // POST: HakemYorumu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PozisyonId,YorumcuAdi,Yorum,Karar")] HakemYorumu hakemYorumu)
        {
            if (ModelState.IsValid)
            {
                hakemYorumu.YorumTarihi = DateTime.Now;
                _context.Add(hakemYorumu);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Pozisyon", new { id = hakemYorumu.PozisyonId });
            }
            return View(hakemYorumu);
        }

        // GET: HakemYorumu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hakemYorumu = await _context.HakemYorumlari.FindAsync(id);
            if (hakemYorumu == null)
            {
                return NotFound();
            }
            return View(hakemYorumu);
        }

        // POST: HakemYorumu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PozisyonId,YorumcuAdi,Yorum,Karar,YorumTarihi")] HakemYorumu hakemYorumu)
        {
            if (id != hakemYorumu.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hakemYorumu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HakemYorumuExists(hakemYorumu.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Pozisyon", new { id = hakemYorumu.PozisyonId });
            }
            return View(hakemYorumu);
        }

        // GET: HakemYorumu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hakemYorumu = await _context.HakemYorumlari
                .Include(h => h.Pozisyon)
                .ThenInclude(p => p.Mac)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (hakemYorumu == null)
            {
                return NotFound();
            }

            return View(hakemYorumu);
        }

        // POST: HakemYorumu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hakemYorumu = await _context.HakemYorumlari.FindAsync(id);
            if (hakemYorumu != null)
            {
                var pozisyonId = hakemYorumu.PozisyonId;
                _context.HakemYorumlari.Remove(hakemYorumu);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Pozisyon", new { id = pozisyonId });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool HakemYorumuExists(int id)
        {
            return _context.HakemYorumlari.Any(e => e.Id == id);
        }
    }
}