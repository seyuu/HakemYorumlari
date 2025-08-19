using HakemYorumlari.Data;
using HakemYorumlari.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Text;

namespace HakemYorumlari.Services
{
    public class HakemYorumuToplamaServisi
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HakemYorumuToplamaServisi> _logger;
        private readonly HttpClient _httpClient;
        private readonly YouTubeScrapingService _youtubeService;
        private readonly TVKanalScrapingService _tvKanalService;
        
        // Türk spor siteleri ve scraping ayarları
        private readonly Dictionary<string, SiteConfig> _sporSiteleri = new()
        {
            ["fanatik"] = new SiteConfig 
            { 
                BaseUrl = "https://www.fanatik.com.tr",
                SearchPath = "/arama?q={0}",
                ArticleSelector = ".news-item",
                TitleSelector = ".news-title",
                ContentSelector = ".news-content",
                LinkSelector = "a"
            },
            ["fotomac"] = new SiteConfig 
            { 
                BaseUrl = "https://www.fotomac.com.tr",
                SearchPath = "/arama/{0}",
                ArticleSelector = ".haber-item",
                TitleSelector = ".haber-baslik",
                ContentSelector = ".haber-ozet",
                LinkSelector = "a"
            },
            ["aspor"] = new SiteConfig 
            { 
                BaseUrl = "https://www.aspor.com.tr",
                SearchPath = "/arama?q={0}",
                ArticleSelector = ".news-list-item",
                TitleSelector = ".title",
                ContentSelector = ".summary",
                LinkSelector = "a"
            },
            ["sabah"] = new SiteConfig 
            { 
                BaseUrl = "https://www.sabah.com.tr",
                SearchPath = "/spor/arama?q={0}",
                ArticleSelector = ".news-item",
                TitleSelector = ".news-title",
                ContentSelector = ".news-summary",
                LinkSelector = "a"
            }
        };
        
        // Hakem yorumcuları ve anahtar kelimeler
        private readonly string[] _hakemYorumculari = {
            "Fırat Aydınus", "Deniz Çoban", "Bahattin Duran", "Bülent Yıldırım",
            "Erman Toroğlu", "Selçuk Dereli", "Cüneyt Çakır", "Hüseyin Göçek",
            "Bünyamin Gezer", "Serdar Tatlı", "Ümit Öztürk"
        };
        
        private readonly string[] _hakemAnahtarKelimeler = {
            "hakem", "penaltı", "VAR", "kırmızı kart", "sarı kart", "ofsayt",
            "faul", "tartışmalı pozisyon", "hatalı karar", "doğru karar"
        };

        public HakemYorumuToplamaServisi(ApplicationDbContext context, 
            ILogger<HakemYorumuToplamaServisi> logger, 
            HttpClient httpClient,
            YouTubeScrapingService youtubeService,
            TVKanalScrapingService tvKanalService)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _youtubeService = youtubeService;
            _tvKanalService = tvKanalService;
            
            // HttpClient ayarları
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<bool> MacIcinYorumTopla(int macId)
        {
            try
            {
                var mac = await _context.Maclar
                    .Include(m => m.Pozisyonlar)
                    .ThenInclude(p => p.HakemYorumlari)
                    .FirstOrDefaultAsync(m => m.Id == macId);

                if (mac == null) 
                {
                    _logger.LogWarning($"Maç bulunamadı: ID {macId}");
                    return false;
                }

                _logger.LogInformation($"Maç için yorum toplama başlatıldı: {mac.EvSahibi} vs {mac.Deplasman} (ID: {macId})");

                var bulunanYorumlar = new List<BulunanYorum>();
                var macBilgisi = $"{mac.EvSahibi} {mac.Deplasman}";

                // 1. YouTube'dan yorum topla (ÖNCELİKLİ)
                _logger.LogInformation("YouTube kanallarından yorum toplama başlatıldı...");
                
                // YouTube servisi null mu kontrol et
                if (_youtubeService == null)
                {
                    _logger.LogError("YouTube servisi NULL! Dependency injection hatası!");
                }
                else
                {
                    _logger.LogInformation("YouTube servisi başarıyla başlatıldı, yorum toplama başlıyor...");
                }
                
                try
                {
                    var youtubeYorumlari = await _youtubeService.MacIcinKanalYorumlariniTopla(macBilgisi, mac.MacTarihi);
                    bulunanYorumlar.AddRange(youtubeYorumlari);
                    _logger.LogInformation($"YouTube kanallarından {youtubeYorumlari.Count} yorum bulundu.");
                    
                    // YouTube'dan yorum geldiyse detaylı log
                    if (youtubeYorumlari.Any())
                    {
                        foreach (var yorum in youtubeYorumlari.Take(3)) // İlk 3 yorumu logla
                        {
                            _logger.LogInformation($"YouTube yorumu: {yorum.YorumcuAdi} - {yorum.Yorum.Substring(0, Math.Min(100, yorum.Yorum.Length))}...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "YouTube kanal yorum toplama hatası");
                }

                // 2. TV Kanalları scraping
                _logger.LogInformation("TV kanallarından yorum toplama başlatıldı...");
                try
                {
                    var tvYorumlari = await _tvKanalService.TumKanallarAra(macBilgisi);
                    bulunanYorumlar.AddRange(tvYorumlari);
                    _logger.LogInformation($"TV kanallarından {tvYorumlari.Count} yorum bulundu.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TV kanalları yorum toplama hatası");
                }

                // 3. Gazete siteleri scraping
                _logger.LogInformation("Gazete sitelerinden yorum toplama başlatıldı...");
                var gazeteYorumlari = new List<BulunanYorum>();
                foreach (var site in _sporSiteleri)
                {
                    try
                    {
                        _logger.LogInformation($"Site taranıyor: {site.Key}");
                        var siteYorumlari = await WebScrapingYap(site.Key, site.Value, macBilgisi, mac.MacTarihi);
                        gazeteYorumlari.AddRange(siteYorumlari);
                        _logger.LogInformation($"{site.Key} sitesinden {siteYorumlari.Count} yorum bulundu.");
                        await Task.Delay(2000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Site {site.Key} tarama hatası");
                    }
                }
                bulunanYorumlar.AddRange(gazeteYorumlari);
                _logger.LogInformation($"Gazete sitelerinden toplam {gazeteYorumlari.Count} yorum bulundu.");

                // 4. Mevcut bağlantılardan yorum çekme
                _logger.LogInformation("Mevcut bağlantılardan yorum çekme başlatıldı...");
                var mevcutBaglantilar = await _context.HakemYorumlari
                    .Where(h => h.Pozisyon.MacId == macId && !string.IsNullOrEmpty(h.KaynakLink))
                    .Select(h => h.KaynakLink)
                    .Distinct()
                    .ToListAsync();
                    
                _logger.LogInformation($"{mevcutBaglantilar.Count} mevcut bağlantı bulundu.");
                
                foreach (var baglantiLink in mevcutBaglantilar)
                {
                    try
                    {
                        var baglantiYorumu = await BaglantiLinktenYorumCek(baglantiLink!);
                        if (baglantiYorumu != null)
                        {
                            bulunanYorumlar.Add(baglantiYorumu);
                            _logger.LogInformation($"Bağlantıdan yorum çekildi: {baglantiLink}");
                        }
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Bağlantı {baglantiLink} işleme hatası");
                    }
                }

                _logger.LogInformation($"Toplam {bulunanYorumlar.Count} yorum bulundu. İşleme başlanıyor...");

                // Yorumları işle ve kaydet
                if (bulunanYorumlar.Any())
                {
                    await YorumlariIsleVeKaydet(mac, bulunanYorumlar);
                    _logger.LogInformation("Yorumlar başarıyla işlendi ve kaydedildi.");
                }
                else
                {
                    _logger.LogWarning("Hiç yorum bulunamadı!");
                }

                // Maç durumunu güncelle
                mac.YorumlarToplandi = true;
                mac.YorumToplamaZamani = DateTime.Now;
                mac.YorumToplamaNotlari = $"{bulunanYorumlar.Count} yorum bulundu";
                
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Maç {macId} için yorum toplama tamamlandı.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Maç {macId} için yorum toplama genel hatası");
                return false;
            }
        }

        private async Task<BulunanYorum?> YouTubeHtmlFallback(string youtubeUrl, string macBilgisi)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(youtubeUrl);
                if (string.IsNullOrWhiteSpace(html)) return null;

                string? title = null;
                // og:title
                var ogMatch = Regex.Match(html, "<meta[^>]*property=\"og:title\"[^>]*content=\"(.*?)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (ogMatch.Success) title = ogMatch.Groups[1].Value;
                // <title>
                if (string.IsNullOrWhiteSpace(title))
                {
                    var tMatch = Regex.Match(html, "<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (tMatch.Success) title = tMatch.Groups[1].Value;
                }
                if (string.IsNullOrWhiteSpace(title)) title = "YouTube Videosu";

                return new BulunanYorum
                {
                    YorumcuAdi = "YouTube",
                    Yorum = title,
                    DogruKarar = true,
                    Kanal = "YouTube",
                    KaynakLink = youtubeUrl,
                    KaynakTuru = "YouTube",
                    BulunduguSite = "YouTube",
                    BulunmaTarihi = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"YouTubeHtmlFallback hata: {youtubeUrl}");
                return null;
            }
        }

        public async Task<bool> MacIcinYouTubeLinktenYorumEkle(int macId, string youtubeUrl)
        {
            try
            {
                var mac = await _context.Maclar
                    .Include(m => m.Pozisyonlar)
                    .ThenInclude(p => p.HakemYorumlari)
                    .FirstOrDefaultAsync(m => m.Id == macId);

                if (mac == null) return false;

                var macBilgisi = $"{mac.EvSahibi} {mac.Deplasman}";
                var yorum = await _youtubeService.VideoLinkindenTekYorum(youtubeUrl, macBilgisi);
                if (yorum == null)
                {
                    _logger.LogWarning($"YouTube linkinden yorum bulunamadı (API). HTML fallback denenecek: {youtubeUrl}");
                    yorum = await YouTubeHtmlFallback(youtubeUrl, macBilgisi);
                    if (yorum == null)
                    {
                        return false;
                    }
                }

                await YorumlariIsleVeKaydet(mac, new List<BulunanYorum> { yorum });

                mac.YorumlarToplandi = true;
                mac.YorumToplamaZamani = DateTime.Now;
                mac.YorumToplamaNotlari = "YouTube linkinden 1 yorum eklendi";
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MacIcinYouTubeLinktenYorumEkle hata: macId={macId}, url={youtubeUrl}");
                return false;
            }
        }

        public async Task<bool> MacIcinYouTubeTranscripttenPozisyonEkle(int macId, string youtubeUrl)
        {
            try
            {
                var mac = await _context.Maclar
                    .Include(m => m.Pozisyonlar)
                    .ThenInclude(p => p.HakemYorumlari)
                    .FirstOrDefaultAsync(m => m.Id == macId);

                if (mac == null) return false;

                var macBilgisi = $"{mac.EvSahibi} {mac.Deplasman}";
                var pozisyonlar = await _youtubeService.TranscripttenPozisyonTespitEt(youtubeUrl, macBilgisi);
                
                if (!pozisyonlar.Any())
                {
                    _logger.LogWarning($"Transcript'ten pozisyon bulunamadı: {youtubeUrl}");
                    return false;
                }

                _logger.LogInformation($"Transcript'ten {pozisyonlar.Count} pozisyon bulundu, kaydediliyor...");

                await YorumlariIsleVeKaydet(mac, pozisyonlar);

                mac.YorumlarToplandi = true;
                mac.YorumToplamaZamani = DateTime.Now;
                mac.YorumToplamaNotlari = $"Transcript'ten {pozisyonlar.Count} pozisyon eklendi";
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MacIcinYouTubeTranscripttenPozisyonEkle hata: macId={macId}, url={youtubeUrl}");
                return false;
            }
        }

        private List<string> GenerateAramaTerimleri(Mac mac)
        {
            var takimlar = new[] { mac.EvSahibi, mac.Deplasman };
            var aramaTerimleri = new List<string>();
            
            // Temel arama terimleri
            aramaTerimleri.Add($"{mac.EvSahibi} {mac.Deplasman}");
            aramaTerimleri.Add($"{mac.EvSahibi} {mac.Deplasman} hakem");
            
            // Hakem yorumcuları ile kombinasyonlar
            foreach (var yorumcu in _hakemYorumculari.Take(3))
            {
                aramaTerimleri.Add($"{mac.EvSahibi} {mac.Deplasman} {yorumcu}");
            }
            
            // Anahtar kelimeler ile kombinasyonlar
            foreach (var anahtar in _hakemAnahtarKelimeler.Take(5))
            {
                aramaTerimleri.Add($"{mac.EvSahibi} {mac.Deplasman} {anahtar}");
            }
            
            return aramaTerimleri;
        }

        private async Task<List<BulunanYorum>> WebScrapingYap(string siteKey, SiteConfig siteConfig, string aramaTerimi, DateTime macTarihi)
        {
            var bulunanYorumlar = new List<BulunanYorum>();
            
            try
            {
                var encodedTerm = Uri.EscapeDataString(aramaTerimi);
                var searchUrl = siteConfig.BaseUrl + string.Format(siteConfig.SearchPath, encodedTerm);
                
                _logger.LogInformation($"Web scraping: {searchUrl}");
                
                var response = await _httpClient.GetStringAsync(searchUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);
                
                var articles = doc.DocumentNode.SelectNodes(siteConfig.ArticleSelector);
                if (articles == null) return bulunanYorumlar;
                
                foreach (var article in articles.Take(5)) // İlk 5 sonuç
                {
                    var titleNode = article.SelectSingleNode(siteConfig.TitleSelector);
                    var contentNode = article.SelectSingleNode(siteConfig.ContentSelector);
                    var linkNode = article.SelectSingleNode(siteConfig.LinkSelector);
                    
                    if (titleNode == null) continue;
                    
                    var title = titleNode.InnerText?.Trim();
                    var content = contentNode?.InnerText?.Trim() ?? "";
                    var link = linkNode?.GetAttributeValue("href", "");
                    
                    // Bağlantıyı tam URL'ye çevir
                    if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                    {
                        link = siteConfig.BaseUrl + (link.StartsWith("/") ? link : "/" + link);
                    }
                    
                    // Hakem yorumu içeriyor mu kontrol et
                    if (IsHakemYorumuIceriyor(title + " " + content))
                    {
                        var yorumcu = ExtractYorumcu(title + " " + content);
                        var dogruKarar = AnalyzeDogruKarar(title + " " + content);
                        
                        bulunanYorumlar.Add(new BulunanYorum
                        {
                            YorumcuAdi = yorumcu ?? "Bilinmeyen",
                            Yorum = title + (string.IsNullOrEmpty(content) ? "" : " - " + content),
                            DogruKarar = dogruKarar,
                            Kanal = siteKey,
                            KaynakLink = link,
                            KaynakTuru = "Gazete",
                            BulunduguSite = siteKey,
                            BulunmaTarihi = DateTime.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Web scraping hatası - Site: {siteKey}, Terim: {aramaTerimi}");
            }
            
            return bulunanYorumlar;
        }
        
        private async Task<BulunanYorum?> BaglantiLinktenYorumCek(string baglantiLink)
        {
            try
            {
                _logger.LogInformation($"Bağlantıdan yorum çekiliyor: {baglantiLink}");
                
                var response = await _httpClient.GetStringAsync(baglantiLink);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);
                
                // Sayfa içeriğini analiz et
                var contentSelectors = new[] { ".content", ".article-content", ".news-content", ".haber-icerik", "article", ".post-content" };
                var content = "";
                
                foreach (var selector in contentSelectors)
                {
                    var contentNode = doc.DocumentNode.SelectSingleNode(selector);
                    if (contentNode != null)
                    {
                        content = contentNode.InnerText?.Trim() ?? "";
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(content)) return null;
                
                // Başlık bul
                var titleSelectors = new[] { "h1", ".title", ".news-title", ".haber-baslik" };
                var title = "";
                
                foreach (var selector in titleSelectors)
                {
                    var titleNode = doc.DocumentNode.SelectSingleNode(selector);
                    if (titleNode != null)
                    {
                        title = titleNode.InnerText?.Trim() ?? "";
                        break;
                    }
                }
                
                // Hakem yorumu içeriyor mu kontrol et
                var fullText = title + " " + content;
                if (!IsHakemYorumuIceriyor(fullText)) return null;
                
                var yorumcu = ExtractYorumcu(fullText);
                var dogruKarar = AnalyzeDogruKarar(fullText);
                var siteName = ExtractSiteName(baglantiLink);
                
                return new BulunanYorum
                {
                    YorumcuAdi = yorumcu ?? "Bilinmeyen",
                    Yorum = string.IsNullOrEmpty(title) ? content.Substring(0, Math.Min(200, content.Length)) : title,
                    DogruKarar = dogruKarar,
                    Kanal = siteName,
                    KaynakLink = baglantiLink,
                    KaynakTuru = DetermineKaynakTuru(siteName),
                    BulunduguSite = siteName,
                    BulunmaTarihi = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Bağlantıdan yorum çekme hatası: {baglantiLink}");
                return null;
            }
        }
        
        private bool IsHakemYorumuIceriyor(string text)
        {
            var lowerText = text.ToLower();
            return _hakemAnahtarKelimeler.Any(anahtar => lowerText.Contains(anahtar.ToLower())) ||
                   _hakemYorumculari.Any(yorumcu => lowerText.Contains(yorumcu.ToLower()));
        }
        
        private string? ExtractYorumcu(string text)
        {
            return _hakemYorumculari.FirstOrDefault(yorumcu => 
                text.Contains(yorumcu, StringComparison.OrdinalIgnoreCase));
        }
        
        private bool AnalyzeDogruKarar(string text)
        {
            var lowerText = text.ToLower();
            var dogruKelimeler = new[] { "doğru", "haklı", "yerinde", "uygun", "isabetli" };
            var yanlisKelimeler = new[] { "yanlış", "hatalı", "uygunsuz", "isabetsiz", "tartışmalı" };
            
            var dogruSayisi = dogruKelimeler.Count(kelime => lowerText.Contains(kelime));
            var yanlisSayisi = yanlisKelimeler.Count(kelime => lowerText.Contains(kelime));
            
            return dogruSayisi > yanlisSayisi;
        }
        
        private string ExtractSiteName(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host.Replace("www.", "").Split('.')[0];
            }
            catch
            {
                return "Bilinmeyen";
            }
        }
        
        private string DetermineKaynakTuru(string siteName)
        {
            return siteName.ToLower() switch
            {
                "youtube" or "youtu" => "YouTube",
                "beinsports" or "trtspor" or "aspor" or "sporx" => "TV_Web",
                _ => "Gazete"
            };
        }

        private async Task YorumlariIsleVeKaydet(Mac mac, List<BulunanYorum> bulunanYorumlar)
        {
            // Benzer yorumları grupla ve pozisyonlara ata
            var yorumGruplari = bulunanYorumlar
                .GroupBy(y => new { y.YorumcuAdi, PozisyonAnahtar = ExtractPozisyonAnahtar(y.Yorum) })
                .ToList();

            foreach (var grup in yorumGruplari)
            {
                var ilkYorum = grup.First();
                
                // Pozisyon oluştur veya bul
                var pozisyon = await PozisyonOlusturVeyaBul(mac, ilkYorum);
                
                // Hakem yorumu ekle
                var hakemYorumu = new HakemYorumu
                {
                    PozisyonId = pozisyon.Id,
                    YorumcuAdi = ilkYorum.YorumcuAdi,
                    Yorum = ilkYorum.Yorum,
                    DogruKarar = ilkYorum.DogruKarar,
                    YorumTarihi = DateTime.Now,
                    Kanal = ilkYorum.Kanal,
                    KaynakLink = ilkYorum.KaynakLink,
                    KaynakTuru = ilkYorum.KaynakTuru
                };
                
                _context.HakemYorumlari.Add(hakemYorumu);
            }
        }

        private string ExtractPozisyonAnahtar(string yorum)
        {
            var lowerYorum = yorum.ToLower();
            if (lowerYorum.Contains("penaltı")) return "Penaltı";
            if (lowerYorum.Contains("kırmızı")) return "Kırmızı Kart";
            if (lowerYorum.Contains("sarı")) return "Sarı Kart";
            if (lowerYorum.Contains("var")) return "VAR";
            if (lowerYorum.Contains("ofsayt")) return "Ofsayt";
            if (lowerYorum.Contains("faul")) return "Faul";
            return "Tartışmalı Pozisyon";
        }

        private async Task<Pozisyon> PozisyonOlusturVeyaBul(Mac mac, BulunanYorum yorum)
        {
            var pozisyonTuru = ExtractPozisyonAnahtar(yorum.Yorum);
            var dakika = ExtractDakika(yorum.Yorum);
            
            var mevcutPozisyon = mac.Pozisyonlar
                .FirstOrDefault(p => p.PozisyonTuru == pozisyonTuru && Math.Abs(p.Dakika - dakika) <= 2);
            
            if (mevcutPozisyon != null)
                return mevcutPozisyon;
            
            var yeniPozisyon = new Pozisyon
            {
                MacId = mac.Id,
                Aciklama = $"Web scraping ile tespit edilen {pozisyonTuru.ToLower()}",
                Dakika = dakika,
                PozisyonTuru = pozisyonTuru,
                VideoUrl = "",
                EmbedVideoUrl = "",
                VideoKaynagi = "Web Scraping"
            };
            
            _context.Pozisyonlar.Add(yeniPozisyon);
            await _context.SaveChangesAsync();
            
            return yeniPozisyon;
        }

        private int ExtractDakika(string yorum)
        {
            var dakikaRegex = new Regex(@"(\d+)\s*[.'\""]*\s*dakika", RegexOptions.IgnoreCase);
            var match = dakikaRegex.Match(yorum);
            
            if (match.Success && int.TryParse(match.Groups[1].Value, out int dakika))
                return dakika;
            
            return Random.Shared.Next(1, 90); // Varsayılan rastgele dakika
        }
    }

    public class BulunanYorum
    {
        public string YorumcuAdi { get; set; } = null!;
        public string Yorum { get; set; } = null!;
        public bool DogruKarar { get; set; }
        public string Kanal { get; set; } = null!;
        public string? KaynakLink { get; set; }
        public string? KaynakTuru { get; set; }
        public string BulunduguSite { get; set; } = null!;
        public DateTime BulunmaTarihi { get; set; }
    }
    
    public class SiteConfig
    {
        public string BaseUrl { get; set; } = null!;
        public string SearchPath { get; set; } = null!;
        public string ArticleSelector { get; set; } = null!;
        public string TitleSelector { get; set; } = null!;
        public string ContentSelector { get; set; } = null!;
        public string LinkSelector { get; set; } = null!;
    }
}