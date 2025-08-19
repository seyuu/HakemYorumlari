using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using HakemYorumlari.Services;

namespace HakemYorumlari.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalMaclar = await _context.Maclar.CountAsync(),
                TotalPozisyonlar = await _context.Pozisyonlar.CountAsync(),
                TotalHakemYorumlari = await _context.HakemYorumlari.CountAsync(),
                TotalOylar = await _context.KullaniciAnketleri.CountAsync()
            };
            
            ViewBag.Stats = stats;
            return View();
        }

        // Maç Yönetimi
        public async Task<IActionResult> Maclar(int? hafta = null)
        {
            // Mevcut haftayı hesapla
            var mevcutHafta = GetCurrentWeek(DateTime.Now);
            
            // Eğer hafta parametresi verilmemişse mevcut haftayı kullan
            // Eğer hafta parametresi verilmişse o haftayı kullan
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
            if (ModelState.IsValid)
            {
                _context.Maclar.Add(mac);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Maç başarıyla eklendi.";
                
                // Mevcut haftada kal
                var mevcutHafta = GetCurrentWeek(DateTime.Now);
                return RedirectToAction("Maclar", new { hafta = mevcutHafta });
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
                var hafta = mac.Hafta;
                return RedirectToAction("Maclar", new { hafta = hafta });
            }
            return View(mac);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MacSil(int id)
        {
            var mac = await _context.Maclar.FindAsync(id);
            var hafta = mac?.Hafta ?? GetCurrentWeek(DateTime.Now);
            
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
            var yorumServisi = HttpContext.RequestServices.GetRequiredService<Services.HakemYorumuToplamaServisi>();
            
            var basarili = await yorumServisi.MacIcinYorumTopla(macId);
            
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
            
            return RedirectToAction("Maclar");
        }

        // Manuel maç durumu güncelleme
        [HttpPost]
        public async Task<IActionResult> MacDurumGuncelle(int macId, MacDurumu yeniDurum)
        {
            var mac = await _context.Maclar.FindAsync(macId);
            if (mac != null)
            {
                mac.Durum = yeniDurum;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Maç durumu '{yeniDurum}' olarak güncellendi.";
            }
            
            return RedirectToAction("Maclar");
        }

        // Gerçek skor çekme
        [HttpPost]
        public async Task<IActionResult> SkorCek(int macId)
        {
            // Redirect için varsayılan: mevcut hafta
            var hafta = GetCurrentWeek(DateTime.Now);
            try
            {
                var mac = await _context.Maclar.FindAsync(macId);
                if (mac == null)
                {
                    TempData["ErrorMessage"] = "Maç bulunamadı.";
                    return RedirectToAction("Maclar", new { hafta });
                }

                // Maçın haftasını yakala ki redirect doğru sayfaya dönsün
                hafta = mac.Hafta;

                var skorServisi = HttpContext.RequestServices.GetRequiredService<SkorCekmeServisi>();
                
                // TFF'den öncelikli olarak skor çekmeye çalış
                var skor = await skorServisi.MacSkoruCek(mac.EvSahibi, mac.Deplasman, mac.MacTarihi);
                
                if (!string.IsNullOrEmpty(skor))
                {
                    mac.Skor = skor;
                    mac.Durum = MacDurumu.Bitti;
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Skor başarıyla güncellendi: {skor}";
                }
                else
                {
                    TempData["WarningMessage"] = "Skor bulunamadı. Maç henüz oynanmamış olabilir veya TFF sitesinden veri çekilemedi.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Skor çekilirken hata oluştu: {ex.Message}";
            }
            
            return RedirectToAction("Maclar", new { hafta = hafta });
        }

        // Toplu skor çekme
        [HttpPost]
        public async Task<IActionResult> TopluSkorCek()
        {
            try
            {
                var gecmisMaclar = await _context.Maclar
                    .Where(m => m.MacTarihi < DateTime.Now && 
                               (m.Skor == "-" || string.IsNullOrEmpty(m.Skor)) &&
                               m.Durum != MacDurumu.Bitti)
                    .ToListAsync();

                var skorServisi = HttpContext.RequestServices.GetRequiredService<SkorCekmeServisi>();
                int basariliSayisi = 0;

                foreach (var mac in gecmisMaclar)
                {
                    var skor = await skorServisi.MacSkoruCek(mac.EvSahibi, mac.Deplasman, mac.MacTarihi);
                    if (!string.IsNullOrEmpty(skor))
                    {
                        mac.Skor = skor;
                        mac.Durum = MacDurumu.Bitti;
                        basariliSayisi++;
                    }
                    
                    // Rate limiting için kısa bekleme
                    await Task.Delay(1000);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{basariliSayisi} maçın skoru başarıyla güncellendi.";
                
                // Mevcut haftada kal
                var mevcutHafta = GetCurrentWeek(DateTime.Now);
                return RedirectToAction("Maclar", new { hafta = mevcutHafta });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Toplu skor çekilirken hata oluştu: {ex.Message}";
            }
            
            return RedirectToAction("Maclar");
        }

        // Hafta bazlı toplu skor çekme
        [HttpPost]
        public async Task<IActionResult> HaftaTopluSkorCek(int hafta)
        {
            try
            {
                // Tüm haftaların maçlarını güncelle (sadece seçili hafta değil)
                var tumMaclar = await _context.Maclar
                    .Where(m => m.Liga == "Süper Lig" &&
                               m.MacTarihi < DateTime.Now && 
                               (m.Skor == "-" || string.IsNullOrEmpty(m.Skor)) &&
                               m.Durum != MacDurumu.Bitti)
                    .ToListAsync();

                var skorServisi = HttpContext.RequestServices.GetRequiredService<SkorCekmeServisi>();
                int basariliSayisi = 0;

                foreach (var mac in tumMaclar)
                {
                    var skor = await skorServisi.MacSkoruCek(mac.EvSahibi, mac.Deplasman, mac.MacTarihi);
                    if (!string.IsNullOrEmpty(skor))
                    {
                        mac.Skor = skor;
                        mac.Durum = MacDurumu.Bitti;
                        basariliSayisi++;
                    }
                    
                    // Rate limiting için kısa bekleme
                    await Task.Delay(1000);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{hafta}. hafta için {basariliSayisi} maçın skoru başarıyla güncellendi.";
                return RedirectToAction("Maclar", new { hafta = hafta });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hafta bazlı skor çekilirken hata oluştu: {ex.Message}";
            }
            
            return RedirectToAction("Maclar", new { hafta = hafta });
        }
        
        // Hafta bazlı toplu yorum toplama
        [HttpPost]
        public async Task<IActionResult> HaftaTopluYorumTopla(int hafta)
        {
            try
            {
                var haftaninMaclari = await _context.Maclar
                    .Where(m => m.Hafta == hafta && 
                               m.Liga == "Süper Lig" &&
                               m.Durum == MacDurumu.Bitti &&
                               !m.YorumlarToplandi)
                    .ToListAsync();

                var yorumServisi = HttpContext.RequestServices.GetRequiredService<HakemYorumuToplamaServisi>();
                int basariliSayisi = 0;

                foreach (var mac in haftaninMaclari)
                {
                    var basarili = await yorumServisi.MacIcinYorumTopla(mac.Id);
                    if (basarili)
                    {
                        basariliSayisi++;
                    }
                    
                    // Rate limiting için bekleme
                    await Task.Delay(2000);
                }

                TempData["SuccessMessage"] = $"{hafta}. hafta için {basariliSayisi} maçın yorumları başarıyla toplandı.";
                return RedirectToAction("Maclar", new { hafta = hafta });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hafta bazlı yorum toplanırken hata oluştu: {ex.Message}";
            }
            
            return RedirectToAction("Maclar", new { hafta = hafta });
        }

        // Toplu maç durumu güncelleme
        [HttpPost]
        public async Task<IActionResult> TopluMacDurumGuncelle(string durumFiltresi, MacDurumu yeniDurum)
        {
            var maclar = _context.Maclar.AsQueryable();
            
            switch (durumFiltresi)
            {
                case "gecmis":
                    maclar = maclar.Where(m => m.MacTarihi < DateTime.Now && m.Durum != MacDurumu.Bitti);
                    break;
                case "bugun":
                    var bugun = DateTime.Today;
                    maclar = maclar.Where(m => m.MacTarihi.Date == bugun && m.Durum != MacDurumu.Bitti);
                    break;
                case "bekliyor":
                    maclar = maclar.Where(m => m.Durum == MacDurumu.Bekliyor);
                    break;
            }
            
            var guncellenecekMaclar = await maclar.ToListAsync();
            
            foreach (var mac in guncellenecekMaclar)
            {
                mac.Durum = yeniDurum;
            }
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"{guncellenecekMaclar.Count} maçın durumu '{yeniDurum}' olarak güncellendi.";
            
            // Mevcut haftada kal
            var mevcutHafta = GetCurrentWeek(DateTime.Now);
            return RedirectToAction("Maclar", new { hafta = mevcutHafta });
        }

        // Maç istatistikleri
        [HttpGet]
        public async Task<IActionResult> MacIstatistikleri()
        {
            var istatistikler = new
            {
                ToplamMac = await _context.Maclar.CountAsync(),
                BekleyenMac = await _context.Maclar.CountAsync(m => m.Durum == MacDurumu.Bekliyor),
                OynananMac = await _context.Maclar.CountAsync(m => m.Durum == MacDurumu.Oynaniyor),
                BitenMac = await _context.Maclar.CountAsync(m => m.Durum == MacDurumu.Bitti),
                ErtelenanMac = await _context.Maclar.CountAsync(m => m.Durum == MacDurumu.Ertelendi),
                YorumToplananMac = await _context.Maclar.CountAsync(m => m.YorumlarToplandi),
                OtomatikYorumAktifMac = await _context.Maclar.CountAsync(m => m.OtomatikYorumToplamaAktif)
            };
            
            return Json(istatistikler);
        }

        // POST: Admin/OtomatikPozisyonTespit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtomatikPozisyonTespit(int macId)
        {
            try
            {
                var pozisyonTespitServisi = HttpContext.RequestServices.GetRequiredService<PozisyonOtomatikTespitServisi>();
                
                // Maç için otomatik pozisyon tespiti yap
                var tespitEdilenPozisyonlar = await pozisyonTespitServisi.MacIcinPozisyonTespitEt(macId);
                
                if (tespitEdilenPozisyonlar.Any())
                {
                    // Pozisyonları veritabanına kaydet
                    var kaydedilenSayi = await pozisyonTespitServisi.PozisyonlariKaydet(tespitEdilenPozisyonlar);
                    
                    // Pozisyon türlerini grupla
                    var pozisyonTurleri = tespitEdilenPozisyonlar
                        .GroupBy(p => p.PozisyonTuru)
                        .Select(g => $"{g.Key}: {g.Count()}")
                        .ToList();
                    
                    var pozisyonDetay = string.Join(", ", pozisyonTurleri);
                    
                    TempData["SuccessMessage"] = $"{kaydedilenSayi} pozisyon otomatik olarak tespit edildi ve kaydedildi. ({pozisyonDetay})";
                }
                else
                {
                    TempData["WarningMessage"] = "Bu maç için pozisyon tespit edilemedi. Yorumlar yeterli değil olabilir.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Pozisyon tespit edilirken bir hata oluştu: " + ex.Message;
            }
            
            // Maçın haftasını bul ve o haftada kal
            var mac = await _context.Maclar.FindAsync(macId);
            var hafta = mac?.Hafta ?? GetCurrentWeek(DateTime.Now);
            
            return RedirectToAction("Maclar", new { hafta = hafta });
        }

        // Hafta hesaplama metodu
        private int GetCurrentWeek(DateTime tarih)
        {
            // TFF'deki hafta sistemi:
            // 08.08.2025 = 1. hafta
            // 15.08.2025 = 2. hafta
            // 22.08.2025 = 3. hafta
            // vs.
            
            // Süper Lig sezon başlangıcı (8 Ağustos 2025)
            var sezonBaslangic = new DateTime(2025, 8, 8);
            
            if (tarih < sezonBaslangic)
                return 1;
                
            var gecenGunler = (tarih - sezonBaslangic).Days;
            var hafta = (int)Math.Ceiling(gecenGunler / 7.0);
            
            return Math.Min(Math.Max(hafta, 1), 38); // 1-38 hafta arası
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

        [HttpPost]
        public async Task<IActionResult> YouTubeLinktenYorumTopla(int macId, string youtubeUrl, int hafta)
        {
            try
            {
                var yorumServisi = HttpContext.RequestServices.GetRequiredService<HakemYorumuToplamaServisi>();
                var basarili = await yorumServisi.MacIcinYouTubeLinktenYorumEkle(macId, youtubeUrl);
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
            }
            return RedirectToAction("Maclar", new { hafta });
        }

        [HttpPost]
        public async Task<IActionResult> YouTubeTranscripttenPozisyonEkle(int macId, string youtubeUrl, int hafta)
        {
            try
            {
                var yorumServisi = HttpContext.RequestServices.GetRequiredService<HakemYorumuToplamaServisi>();
                var basarili = await yorumServisi.MacIcinYouTubeTranscripttenPozisyonEkle(macId, youtubeUrl);
                
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
            }
            
            return RedirectToAction("Maclar", new { hafta });
        }
    }
}