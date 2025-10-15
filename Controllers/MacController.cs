using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using HakemYorumlari.Helpers;

namespace HakemYorumlari.Controllers
{
    public class MacController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MacController> _logger;

        public MacController(ApplicationDbContext context, ILogger<MacController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Mac
        public async Task<IActionResult> Index(string liga = "", int hafta = 0)
        {
            // Aktif haftayı belirle (bugünün tarihine göre)
            var bugun = DateTime.Now.Date;
            var aktifHafta = HaftaHelper.GetCurrentWeek(bugun);
            
            // Eğer hafta seçilmemişse aktif haftayı göster
            if (hafta == 0)
            {
                hafta = aktifHafta;
            }

            var maclarQuery = _context.Maclar
                .Include(m => m.Pozisyonlar)
                .ThenInclude(p => p.HakemYorumlari)
                .AsQueryable();

            if (!string.IsNullOrEmpty(liga))
            {
                maclarQuery = maclarQuery.Where(m => m.Liga.Contains(liga));
            }

            if (hafta > 0)
            {
                maclarQuery = maclarQuery.Where(m => m.Hafta == hafta);
            }

            var maclar = await maclarQuery
                .OrderBy(m => m.MacTarihi)
                .ToListAsync();

            // Filtre için gerekli veriler
            ViewBag.Ligalar = await _context.Maclar
                .Select(m => m.Liga)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            ViewBag.Haftalar = await _context.Maclar
                .Select(m => m.Hafta)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();

            ViewBag.SecilenLiga = liga;
            ViewBag.SecilenHafta = hafta;
            ViewBag.AktifHafta = aktifHafta;

            return View(maclar);
        }

        // GET: Mac/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mac = await _context.Maclar
                .Include(m => m.Pozisyonlar)
                .ThenInclude(p => p.HakemYorumlari)
                .Include(m => m.Pozisyonlar)
                .ThenInclude(p => p.KullaniciAnketleri)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mac == null)
            {
                return NotFound();
            }

            return View(mac);
        }

        // GET: Mac/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Mac/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Mac mac)
        {
            // Navigation property'leri ModelState'den çıkar
            ModelState.Remove("Pozisyonlar");
            ModelState.Remove("YorumToplamaNotlari");
            
            if (ModelState.IsValid)
            {
                _context.Add(mac);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Yeni maç eklendi: {EvSahibi} vs {Deplasman}", mac.EvSahibi, mac.Deplasman);
                return RedirectToAction(nameof(Index));
            }
            return View(mac);
        }

        // GET: Mac/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mac = await _context.Maclar.FindAsync(id);
            if (mac == null)
            {
                return NotFound();
            }
            return View(mac);
        }

        // POST: Mac/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Mac mac)
        {
            if (id != mac.Id)
            {
                return NotFound();
            }

            // Navigation property'leri ModelState'den çıkar
            ModelState.Remove("Pozisyonlar");
            ModelState.Remove("YorumToplamaNotlari");
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mac);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Maç güncellendi: ID {MacId}", mac.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MacExists(mac.Id))
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
            return View(mac);
        }

        // GET: Mac/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mac = await _context.Maclar
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mac == null)
            {
                return NotFound();
            }

            return View(mac);
        }

        // POST: Mac/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mac = await _context.Maclar.FindAsync(id);
            if (mac != null)
            {
                _context.Maclar.Remove(mac);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Maç silindi: ID {MacId}", id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MacExists(int id)
        {
            return _context.Maclar.Any(e => e.Id == id);
        }
    }
}