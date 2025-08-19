using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using System.Diagnostics;

namespace HakemYorumlari.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Aktif haftayı belirle (bugünün tarihine en yakın hafta)
            var bugun = DateTime.Now.Date;
            var aktifHafta = await BelirleAktifHafta(bugun);
            
            // Bu haftanın maçlarını getir
            var buHaftaninMaclari = await _context.Maclar
                .Include(m => m.Pozisyonlar)
                .ThenInclude(p => p.HakemYorumlari)
                .Where(m => m.Hafta == aktifHafta)
                .OrderBy(m => m.MacTarihi)
                .ToListAsync();

            // Eğer bu haftanın maçları yoksa, son oynanan maçları göster
            if (!buHaftaninMaclari.Any())
            {
                buHaftaninMaclari = await _context.Maclar
                    .Include(m => m.Pozisyonlar)
                    .ThenInclude(p => p.HakemYorumlari)
                    .Where(m => m.MacTarihi <= bugun)
                    .OrderByDescending(m => m.MacTarihi)
                    .Take(10)
                    .ToListAsync();
            }

            var toplamPozisyon = await _context.Pozisyonlar.CountAsync();
            var toplamYorum = await _context.HakemYorumlari.CountAsync();
            var toplamOy = await _context.KullaniciAnketleri.CountAsync();

            ViewBag.ToplamPozisyon = toplamPozisyon;
            ViewBag.ToplamYorum = toplamYorum;
            ViewBag.ToplamOy = toplamOy;
            ViewBag.AktifHafta = aktifHafta;

            return View(buHaftaninMaclari);
        }
        
        private Task<int> BelirleAktifHafta(DateTime bugun)
        {
            // TFF'deki hafta sistemi:
            // 08.08.2025 = 1. hafta
            // 15.08.2025 = 2. hafta
            // 22.08.2025 = 3. hafta
            // vs.
            
            // Süper Lig sezon başlangıcı (8 Ağustos 2025)
            var sezonBaslangici = new DateTime(2025, 8, 8);
            
            if (bugun < sezonBaslangici)
                return Task.FromResult(1);
                
            var gecenGunler = (bugun - sezonBaslangici).Days;
            var hafta = (int)Math.Ceiling(gecenGunler / 7.0);
            
            return Task.FromResult(Math.Min(Math.Max(hafta, 1), 38)); // 1-38 hafta arası
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
