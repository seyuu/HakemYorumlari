using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using HakemYorumlari.Services;
using HakemYorumlari.ViewModels;
using HakemYorumlari.Helpers;

namespace HakemYorumlari.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly FiksturGuncellemeServisi _fiksturGuncellemeServisi;
        private readonly YouTubeScrapingService _youtubeService;
        private readonly HakemYorumuToplamaServisi _hakemYorumuToplamaServisi;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, 
                      FiksturGuncellemeServisi fiksturGuncellemeServisi,
                      YouTubeScrapingService youtubeService,
                      HakemYorumuToplamaServisi hakemYorumuToplamaServisi,
                      IBackgroundJobService backgroundJobService,
                      ILogger<AdminController> logger)
        {
            _context = context;
            _fiksturGuncellemeServisi = fiksturGuncellemeServisi;
            _youtubeService = youtubeService;
            _hakemYorumuToplamaServisi = hakemYorumuToplamaServisi;
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalMaclar = await _context.Maclar.CountAsync(),
                TotalPozisyonlar = await _context.Pozisyonlar.CountAsync(),
                TotalHakemYorumlari = await _context.HakemYorumlari.CountAsync(),
                TotalOylar = await _context.KullaniciAnketleri.CountAsync()
            };
            
            return View(model);
        }

        // Maç Yönetimi
        public async Task<IActionResult> Maclar(int? hafta = null)
        {
            // Mevcut haftayı hesapla
            var mevcutHafta = HaftaHelper.GetCurrentWeek(DateTime.Now);
            
            // Eğer hafta parametresi verilmemişse mevcut haftayı kullan
            var secilenHafta = hafta ?? mevcutHafta;
            
            // Tüm haftaları al (dropdown için)
            var tumHaftalar = await _context.Maclar
                .Where(m => m.Liga == "Süper Lig")
                .Select(m => m.Hafta)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();
            
            // Seçilen haftanın maçlarını al
            var maclar = await _context.Maclar
                .Include(m => m.Pozisyonlar)
                .Where(m => m.Hafta == secilenHafta && m.Liga == "Süper Lig")
                .OrderBy(m => m.MacTarihi)
                .ToListAsync();
            
            ViewBag.TumHaftalar = tumHaftalar;
            ViewBag.SecilenHafta = secilenHafta;
            ViewBag.MevcutHafta = mevcutHafta;
            
            return View(maclar);
        }

        [HttpGet]
        public IActionResult MacEkle()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MacEkle(Mac mac)
        {
            ModelState.Remove("Pozisyonlar");
            ModelState.Remove("YorumToplamaNotlari");
            
            if (ModelState.IsValid)
            {
                _context.Maclar.Add(mac);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Maç başarıyla eklendi.";
                
                // Maçın haftasında kal
                return RedirectToAction("Maclar", new { hafta = mac.Hafta });
            }
            return View(mac);
        }

        [HttpGet]
        public async Task<IActionResult> MacDuzenle(int id)
        {
            var mac = await _context.Maclar.FindAsync(id);
            if (mac == null)
            {
                return NotFound();
            }
            return View(mac);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MacDuzenle(int id, Mac mac)
        {
            if (id != mac.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Pozisyonlar");
            ModelState.Remove("YorumToplamaNotlari");
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mac);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Maç başarıyla güncellendi.";
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
                
                // Maçın haftasında kal
                return RedirectToAction("Maclar", new { hafta = mac.Hafta });
            }
            return View(mac);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MacSil(int id)
        {
            var mac = await _context.Maclar.FindAsync(id);
            var hafta = mac?.Hafta ?? HaftaHelper.GetCurrentWeek(DateTime.Now);
            
            if (mac != null)
            {
                _context.Maclar.Remove(mac);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Maç başarıyla silindi.";
            }
            
            // Maçın haftasında kal
            return RedirectToAction("Maclar", new { hafta = hafta });
        }

        // Pozisyon Yönetimi
        public async Task<IActionResult> Pozisyonlar()
        {
            var pozisyonlar = await _context.Pozisyonlar
                .Include(p => p.Mac)
                .Include(p => p.HakemYorumlari)
                .Include(p => p.KullaniciAnketleri)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            return View(pozisyonlar);
        }

        [HttpGet]
        public async Task<IActionResult> PozisyonEkle()
        {
            ViewBag.Maclar = await _context.Maclar
                .OrderByDescending(m => m.MacTarihi)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PozisyonEkle(Pozisyon pozisyon)
        {
            ModelState.Remove("Mac");
            ModelState.Remove("HakemYorumlari");
            ModelState.Remove("KullaniciAnketleri");
            
            if (ModelState.IsValid)
            {
                _context.Pozisyonlar.Add(pozisyon);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pozisyon başarıyla eklendi.";
                return RedirectToAction(nameof(Pozisyonlar));
            }
            
            ViewBag.Maclar = await _context.Maclar
                .OrderByDescending(m => m.MacTarihi)
                .ToListAsync();
            return View(pozisyon);
        }

        [HttpGet]
        public async Task<IActionResult> PozisyonDuzenle(int id)
        {
            var pozisyon = await _context.Pozisyonlar.FindAsync(id);
            if (pozisyon == null)
            {
                return NotFound();
            }
            
            ViewBag.Maclar = await _context.Maclar
                .OrderByDescending(m => m.MacTarihi)
                .ToListAsync();
            return View(pozisyon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PozisyonDuzenle(int id, Pozisyon pozisyon)
        {
            if (id != pozisyon.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Mac");
            ModelState.Remove("HakemYorumlari");
            ModelState.Remove("KullaniciAnketleri");
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pozisyon);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Pozisyon başarıyla güncellendi.";
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
                return RedirectToAction(nameof(Pozisyonlar));
            }
            
            ViewBag.Maclar = await _context.Maclar
                .OrderByDescending(m => m.MacTarihi)
                .ToListAsync();
            return View(pozisyon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PozisyonSil(int id)
        {
            var pozisyon = await _context.Pozisyonlar.FindAsync(id);
            if (pozisyon != null)
            {
                _context.Pozisyonlar.Remove(pozisyon);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Pozisyon başarıyla silindi.";
            }
            return RedirectToAction(nameof(Pozisyonlar));
        }

        // Hakem Yorumu Yönetimi
        public async Task<IActionResult> HakemYorumlari()
        {
            var yorumlar = await _context.HakemYorumlari
                .Include(h => h.Pozisyon)
                .ThenInclude(p => p.Mac)
                .OrderByDescending(h => h.Id)
                .ToListAsync();
            return View(yorumlar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HakemYorumuSil(int id)
        {
            var yorum = await _context.HakemYorumlari.FindAsync(id);
            if (yorum != null)
            {
                _context.HakemYorumlari.Remove(yorum);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Hakem yorumu başarıyla silindi.";
            }
            return RedirectToAction(nameof(HakemYorumlari));
        }

        // Kullanıcı Oyları
        public async Task<IActionResult> KullaniciOylari()
        {
            var oylar = await _context.KullaniciAnketleri
                .Include(k => k.Pozisyon)
                .ThenInclude(p => p.Mac)
                .OrderByDescending(k => k.Id)
                .ToListAsync();
            return View(oylar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OyuSil(int id)
        {
            var oy = await _context.KullaniciAnketleri.FindAsync(id);
            if (oy != null)
            {
                _context.KullaniciAnketleri.Remove(oy);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kullanıcı oyu başarıyla silindi.";
            }
            return RedirectToAction(nameof(KullaniciOylari));
        }

        // Yorum Toplama
        [HttpPost]
        public async Task<IActionResult> YorumTopla(int macId)
        {
            var basarili = await _hakemYorumuToplamaServisi.MacIcinYorumTopla(macId);
            
            if (basarili)
            {
                TempData["Success"] = "Hakem yorumları başarıyla toplandı!";
            }
            else
            {
                TempData["Error"] = "Yorum toplama sırasında hata oluştu.";
            }
            
            return RedirectToAction("Maclar");
        }

        [HttpPost]
        public async Task<IActionResult> OtomatikYorumToplamaToggle(int macId)
        {
            var mac = await _context.Maclar.FindAsync(macId);
            if (mac != null)
            {
                mac.OtomatikYorumToplamaAktif = !mac.OtomatikYorumToplamaAktif;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Otomatik yorum toplama {(mac.OtomatikYorumToplamaAktif ? "aktif" : "pasif")} edildi.";
            }
            
            return RedirectToAction("Maclar", new { hafta = mac?.Hafta });
        }

        [HttpPost]
        public async Task<IActionResult> OtomatikPozisyonTespit(int macId)
        {
            try
            {
                var mac = await _context.Maclar
                    .Include(m => m.Pozisyonlar)
                    .FirstOrDefaultAsync(m => m.Id == macId);
                    
                if (mac == null)
                {
                    TempData["ErrorMessage"] = "Maç bulunamadı!";
                    return RedirectToAction("Maclar");
                }
                
                // Pozisyon tespiti servisini çağır
                var pozisyonDetay = await _hakemYorumuToplamaServisi.PozisyonlariOtomatikTespitEt(macId);
                
                if (pozisyonDetay != null && pozisyonDetay.Contains("dakika"))
                {
                    TempData["SuccessMessage"] = $"Pozisyon tespit edildi: {pozisyonDetay.Substring(0, Math.Min(pozisyonDetay.Length, 100))}...";
                    _logger.LogInformation($"Maç {macId} için pozisyon tespit edildi: {pozisyonDetay}");
                }
                else
                {
                    TempData["WarningMessage"] = "Bu maç için pozisyon tespit edilemedi. Yorumlar yeterli değil olabilir.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Pozisyon tespit edilirken bir hata oluştu: " + ex.Message;
                _logger.LogError(ex, "Pozisyon tespit hatası");
            }
            
            // Maçın haftasını bul ve o haftada kal
            var macBilgi = await _context.Maclar.FindAsync(macId);
            var hafta = macBilgi?.Hafta ?? HaftaHelper.GetCurrentWeek(DateTime.Now);
            
            return RedirectToAction("Maclar", new { hafta = hafta });
        }

        [HttpPost]
        public async Task<IActionResult> YouTubeLinktenYorumTopla(int macId, string youtubeUrl, int hafta)
        {
            try
            {
                var basarili = await _hakemYorumuToplamaServisi.MacIcinYouTubeLinktenYorumEkle(macId, youtubeUrl);
                if (basarili)
                {
                    TempData["Success"] = "YouTube linkinden yorum eklendi.";
                }
                else
                {
                    TempData["Warning"] = "YouTube linkinden yorum bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "YouTube linkinden yorum eklenirken hata: " + ex.Message;
                _logger.LogError(ex, "YouTube yorum ekleme hatası");
            }
            return RedirectToAction("Maclar", new { hafta });
        }

        [HttpPost]
        public async Task<IActionResult> YouTubeTranscripttenPozisyonEkle(int macId, string youtubeUrl, int hafta)
        {
            try
            {
                var basarili = await _hakemYorumuToplamaServisi.MacIcinYouTubeTranscripttenPozisyonEkle(macId, youtubeUrl);
                
                if (basarili)
                {
                    TempData["SuccessMessage"] = "YouTube transcript'ten pozisyonlar eklendi!";
                }
                else
                {
                    TempData["WarningMessage"] = "Transcript'ten pozisyon bulunamadı.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Transcript'ten pozisyon eklenirken hata: " + ex.Message;
                _logger.LogError(ex, "Transcript pozisyon ekleme hatası");
            }
            
            return RedirectToAction("Maclar", new { hafta });
        }

        // Fikstür Güncelleme
        [HttpPost]
        public async Task<IActionResult> FiksturuGuncelle()
        {
            try
            {
                var result = await _fiksturGuncellemeServisi.TFFFiksturunuGuncelle();
                
                if (result)
                {
                    TempData["SuccessMessage"] = "TFF fikstürü başarıyla güncellendi!";
                }
                else
                {
                    TempData["ErrorMessage"] = "TFF fikstürü güncellenirken hata oluştu!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Fikstür güncellenirken hata: " + ex.Message;
                _logger.LogError(ex, "Fikstür güncelleme hatası");
            }
            
            return RedirectToAction("Index");
        }

        // Job durumu kontrol endpoint'leri
        [HttpGet]
        public IActionResult JobDurumu()
        {
            // AJAX request ise JSON döndür
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                var jobStatuses = _backgroundJobService.GetAllJobStatuses();
                return Json(jobStatuses);
            }
            
            // Normal request ise View döndür
            var aktifJoblar = _backgroundJobService.GetAllActiveJobs();
            return View(aktifJoblar);
        }
        
        // API endpoint
        [HttpGet]
        [Route("Admin/Api/JobStatus")]
        public IActionResult GetAllJobStatuses()
        {
            var jobStatuses = _backgroundJobService.GetAllJobStatuses();
            return Json(jobStatuses);
        }

        // İptal endpointini ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/CancelJob")]
        public IActionResult CancelJob([FromBody] CancelJobRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.JobId))
            {
                return Json(new { success = false, message = "Geçersiz jobId" });
            }

            var success = _backgroundJobService.CancelJob(request.JobId);
            return Json(new { success, message = success ? "İşlem iptal edildi" : "İptal edilecek aktif işlem bulunamadı" });
        }

        [HttpPost]
        public async Task<IActionResult> StartHaftaYorumToplama([FromBody] HaftaYorumToplamaRequest request)
        {
            try
            {
                var jobId = _backgroundJobService.EnqueueHaftaYorumToplama(request.Hafta);
                return Json(new { success = true, jobId, message = $"Hafta {request.Hafta} için yorum toplama başlatıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hafta yorum toplama başlatma hatası");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper metodlar
        private bool MacExists(int id)
        {
            return _context.Maclar.Any(e => e.Id == id);
        }

        private bool PozisyonExists(int id)
        {
            return _context.Pozisyonlar.Any(e => e.Id == id);
        }
    }

    // Request model
    public class HaftaYorumToplamaRequest
    {
        public int Hafta { get; set; }
    }

    // İptal için request modeli
    public class CancelJobRequest
    {
        public string JobId { get; set; } = string.Empty;
    }
}