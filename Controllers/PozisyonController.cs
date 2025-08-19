using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Models;

namespace HakemYorumlari.Controllers
{
    public class PozisyonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PozisyonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pozisyon
        public async Task<IActionResult> Index()
        {
            var pozisyonlar = await _context.Pozisyonlar
                .Include(p => p.Mac)
                .Include(p => p.HakemYorumlari)
                .Include(p => p.KullaniciAnketleri)
                .OrderByDescending(p => p.Mac.MacTarihi)
                .ThenBy(p => p.Dakika)
                .ToListAsync();

            return View(pozisyonlar);
        }

        // GET: Pozisyon/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pozisyon = await _context.Pozisyonlar
                .Include(p => p.Mac)
                .Include(p => p.HakemYorumlari)
                .Include(p => p.KullaniciAnketleri)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pozisyon == null)
            {
                return NotFound();
            }

            // Kullanıcının daha önce oy verip vermediğini kontrol et
            var kullaniciIp = GetClientIpAddress();
            var mevcutOy = await _context.KullaniciAnketleri
                .FirstOrDefaultAsync(k => k.PozisyonId == id && k.KullaniciIp == kullaniciIp);

            ViewBag.KullaniciOyVermiş = mevcutOy != null;
            ViewBag.KullaniciOyu = mevcutOy?.DogruKarar;

            // Anket sonuçlarını hesapla
            var toplamOy = pozisyon.KullaniciAnketleri.Count;
            var dogruOylar = pozisyon.KullaniciAnketleri.Count(k => k.DogruKarar);
            var yanlisOylar = toplamOy - dogruOylar;

            ViewBag.ToplamOy = toplamOy;
            ViewBag.DogruOylar = dogruOylar;
            ViewBag.YanlisOylar = yanlisOylar;
            ViewBag.DogruYuzde = toplamOy > 0 ? (dogruOylar * 100.0 / toplamOy) : 0;
            ViewBag.YanlisYuzde = toplamOy > 0 ? (yanlisOylar * 100.0 / toplamOy) : 0;

            return View(pozisyon);
        }

        // POST: Pozisyon/Vote
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vote(int pozisyonId, bool dogruKarar)
        {
            var kullaniciIp = GetClientIpAddress();
            
            // Kullanıcının daha önce oy verip vermediğini kontrol et
            var mevcutOy = await _context.KullaniciAnketleri
                .FirstOrDefaultAsync(k => k.PozisyonId == pozisyonId && k.KullaniciIp == kullaniciIp);

            if (mevcutOy != null)
            {
                // Mevcut oyu güncelle
                mevcutOy.DogruKarar = dogruKarar;
                mevcutOy.OyTarihi = DateTime.Now;
                _context.Update(mevcutOy);
            }
            else
            {
                // Yeni oy ekle
                var yeniOy = new KullaniciAnketi
                {
                    PozisyonId = pozisyonId,
                    KullaniciIp = kullaniciIp,
                    DogruKarar = dogruKarar,
                    OyTarihi = DateTime.Now
                };
                _context.KullaniciAnketleri.Add(yeniOy);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = pozisyonId });
        }

        // GET: Pozisyon/Create
        public async Task<IActionResult> Create(int? macId)
        {
            if (macId.HasValue)
            {
                var mac = await _context.Maclar.FindAsync(macId.Value);
                if (mac != null)
                {
                    ViewBag.MacBilgisi = $"{mac.EvSahibi} vs {mac.Deplasman}";
                    ViewBag.MacId = macId.Value;
                }
            }

            ViewBag.Maclar = await _context.Maclar
                .OrderByDescending(m => m.MacTarihi)
                .Select(m => new { m.Id, Takim = $"{m.EvSahibi} vs {m.Deplasman} ({m.MacTarihi:dd.MM.yyyy})" })
                .ToListAsync();

            return View();
        }

        // POST: Pozisyon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,MacId,Aciklama,Dakika,PozisyonTuru,VideoUrl,EmbedVideoUrl,VideoKaynagi,HakemKarari,TartismaDerecesi")] Pozisyon pozisyon)
        {
            // MacId kontrolü
            if (pozisyon.MacId <= 0)
            {
                var errorMessage = "Maç ID bulunamadı. Lütfen tekrar deneyin.";
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                ModelState.AddModelError("MacId", errorMessage);
                return View(pozisyon); // Hemen return et
            }
            
            // Model validation'ı kontrol et
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                var errorMessage = string.Join(", ", errors);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = errorMessage });
                }
                
                // ViewBag'leri tekrar set et
                if (pozisyon.MacId > 0)
                {
                    var mac = await _context.Maclar.FindAsync(pozisyon.MacId);
                    if (mac != null)
                    {
                        ViewBag.MacBilgisi = $"{mac.EvSahibi} vs {mac.Deplasman}";
                        ViewBag.MacId = pozisyon.MacId;
                    }
                }

                ViewBag.Maclar = await _context.Maclar
                    .OrderByDescending(m => m.MacTarihi)
                    .Select(m => new { m.Id, Takim = $"{m.EvSahibi} vs {m.Deplasman} ({m.MacTarihi:dd.MM.yyyy})" })
                    .ToListAsync();

                return View(pozisyon);
            }

            try
            {
                // Varsayılan değerleri set et
                if (string.IsNullOrEmpty(pozisyon.VideoKaynagi))
                    pozisyon.VideoKaynagi = "beIN Sports";
                
                if (pozisyon.TartismaDerecesi == 0)
                    pozisyon.TartismaDerecesi = 5;

                _context.Add(pozisyon);
                await _context.SaveChangesAsync();
                
                // ViewBag'leri tekrar set et
                if (pozisyon.MacId > 0)
                {
                    var mac = await _context.Maclar.FindAsync(pozisyon.MacId);
                    if (mac != null)
                    {
                        ViewBag.MacBilgisi = $"{mac.EvSahibi} vs {mac.Deplasman}";
                        ViewBag.MacId = pozisyon.MacId;
                    }
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Pozisyon başarıyla eklendi", redirectUrl = Url.Action("Details", "Mac", new { id = pozisyon.MacId }) });
                }
                
                return RedirectToAction("Details", "Mac", new { id = pozisyon.MacId });
            }
            catch (Exception ex)
            {
                // ViewBag'leri tekrar set et
                if (pozisyon.MacId > 0)
                {
                    var mac = await _context.Maclar.FindAsync(pozisyon.MacId);
                    if (mac != null)
                    {
                        ViewBag.MacBilgisi = $"{mac.EvSahibi} vs {mac.Deplasman}";
                        ViewBag.MacId = pozisyon.MacId;
                    }
                }

                ViewBag.Maclar = await _context.Maclar
                    .OrderByDescending(m => m.MacTarihi)
                    .Select(m => new { m.Id, Takim = $"{m.EvSahibi} vs {m.Deplasman} ({m.MacTarihi:dd.MM.yyyy})" })
                    .ToListAsync();

                return View(pozisyon);
            }
        }

        // GET: Pozisyon/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pozisyon = await _context.Pozisyonlar
                .Include(p => p.Mac)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pozisyon == null)
            {
                return NotFound();
            }

            ViewBag.Maclar = await _context.Maclar
                .OrderByDescending(m => m.MacTarihi)
                .Select(m => new { m.Id, Takim = $"{m.EvSahibi} vs {m.Deplasman} ({m.MacTarihi:dd.MM.yyyy})" })
                .ToListAsync();

            return View(pozisyon);
        }

        // POST: Pozisyon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MacId,Aciklama,Dakika,PozisyonTuru,VideoUrl")] Pozisyon pozisyon)
        {
            if (id != pozisyon.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pozisyon);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PozisyonExists(pozisyon.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Mac", new { id = pozisyon.MacId });
            }

            ViewBag.Maclar = await _context.Maclar
                .OrderByDescending(m => m.MacTarihi)
                .Select(m => new { m.Id, Takim = $"{m.EvSahibi} vs {m.Deplasman} ({m.MacTarihi:dd.MM.yyyy})" })
                .ToListAsync();

            return View(pozisyon);
        }

        // GET: Pozisyon/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pozisyon = await _context.Pozisyonlar
                .Include(p => p.Mac)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pozisyon == null)
            {
                return NotFound();
            }

            return View(pozisyon);
        }

        // POST: Pozisyon/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pozisyon = await _context.Pozisyonlar.FindAsync(id);
            if (pozisyon != null)
            {
                _context.Pozisyonlar.Remove(pozisyon);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PozisyonExists(int id)
        {
            return _context.Pozisyonlar.Any(e => e.Id == id);
        }

        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1"; // localhost için
            }
            return ipAddress ?? "unknown";
        }
    }
}