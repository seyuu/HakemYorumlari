using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Models;

namespace HakemYorumlari.Controllers
{
    public class PozisyonController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PozisyonController> _logger;

        public PozisyonController(ApplicationDbContext context, ILogger<PozisyonController> logger)
        {
            _context = context;
            _logger = logger;
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

        // GET: Pozisyon/Create
        public async Task<IActionResult> Create(int? macId = null)
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
        public async Task<IActionResult> Create(Pozisyon pozisyon)
        {
            try
            {
                // Navigation property'leri ModelState'den çıkar
                ModelState.Remove("Mac");
                ModelState.Remove("HakemYorumlari");
                ModelState.Remove("KullaniciAnketleri");
                ModelState.Remove("VideoKaynagi");
                ModelState.Remove("HakemKarari");
                ModelState.Remove("EmbedVideoUrl");

                if (!ModelState.IsValid)
                {
                    // ModelState hatalarını logla
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Model doğrulama hatası: {ErrorMessage}", modelError.ErrorMessage);
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

                // Pozisyonu kaydet
                _context.Add(pozisyon);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Yeni pozisyon eklendi: Maç {MacId}, Dakika {Dakika}", pozisyon.MacId, pozisyon.Dakika);

                // AJAX isteği için JSON response
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Pozisyon başarıyla eklendi", redirectUrl = Url.Action("Details", "Mac", new { id = pozisyon.MacId }) });
                }
                
                return RedirectToAction("Details", "Mac", new { id = pozisyon.MacId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pozisyon eklenirken hata oluştu");
                
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

                ModelState.AddModelError("", "Pozisyon kaydedilirken bir hata oluştu.");
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
        public async Task<IActionResult> Edit(int id, Pozisyon pozisyon)
        {
            if (id != pozisyon.Id)
            {
                return NotFound();
            }

            // Navigation property'leri ModelState'den çıkar
            ModelState.Remove("Mac");
            ModelState.Remove("HakemYorumlari");
            ModelState.Remove("KullaniciAnketleri");
            ModelState.Remove("VideoKaynagi");
            ModelState.Remove("HakemKarari");
            ModelState.Remove("EmbedVideoUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pozisyon);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Pozisyon güncellendi: ID {PozisyonId}", pozisyon.Id);
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
                var macId = pozisyon.MacId;
                _context.Pozisyonlar.Remove(pozisyon);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Pozisyon silindi: ID {PozisyonId}", id);
                return RedirectToAction("Details", "Mac", new { id = macId });
            }

            return RedirectToAction(nameof(Index));
        }

        // Kullanıcı Anketi - Oy Verme
        [HttpPost]
        public async Task<IActionResult> OyVer(int pozisyonId, bool dogruKarar)
        {
            var kullaniciIp = GetClientIpAddress();

            // Daha önce oy verilmiş mi kontrol et
            var mevcutOy = await _context.KullaniciAnketleri
                .FirstOrDefaultAsync(k => k.PozisyonId == pozisyonId && k.KullaniciIp == kullaniciIp);

            if (mevcutOy != null)
            {
                return Json(new { success = false, message = "Bu pozisyon için zaten oy kullandınız." });
            }

            var yeniOy = new KullaniciAnketi
            {
                PozisyonId = pozisyonId,
                DogruKarar = dogruKarar,
                KullaniciIp = kullaniciIp,
                OyTarihi = DateTime.Now
            };

            _context.KullaniciAnketleri.Add(yeniOy);
            await _context.SaveChangesAsync();

            // Güncel sonuçları hesapla
            var toplamOy = await _context.KullaniciAnketleri.CountAsync(k => k.PozisyonId == pozisyonId);
            var dogruOylar = await _context.KullaniciAnketleri.CountAsync(k => k.PozisyonId == pozisyonId && k.DogruKarar);

            return Json(new
            {
                success = true,
                toplamOy = toplamOy,
                dogruOylar = dogruOylar,
                yanlisOylar = toplamOy - dogruOylar
            });
        }

        // Helper metodlar
        private bool PozisyonExists(int id)
        {
            return _context.Pozisyonlar.Any(e => e.Id == id);
        }

        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            
            // Proxy arkasındaysa gerçek IP'yi al
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = Request.Headers["X-Forwarded-For"].ToString().Split(',').FirstOrDefault()?.Trim();
            }
            else if (Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = Request.Headers["X-Real-IP"].ToString();
            }
            
            return ipAddress ?? "Unknown";
        }
    }
}