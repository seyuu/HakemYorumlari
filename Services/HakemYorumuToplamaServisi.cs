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
        // Otomatik pozisyon tespit servisi (DI)
        private readonly PozisyonOtomatikTespitServisi _pozisyonTespitServisi;

        // Gazete/sport siteleri scraping konfigürasyonları
        // Eksik alanlar eklendi
        private readonly Dictionary<string, SiteConfig> _sporSiteleri = new()
        {
            ["Fanatik"] = new SiteConfig
            {
                BaseUrl = "https://www.fanatik.com.tr",
                SearchPath = "/arama?q={0}",
                ArticleSelector = "article",
                TitleSelector = "h2",
                ContentSelector = "p",
                LinkSelector = "a"
            },
            ["SporX"] = new SiteConfig
            {
                BaseUrl = "https://www.sporx.com",
                SearchPath = "/search?q={0}",
                ArticleSelector = "article",
                TitleSelector = "h2",
                ContentSelector = "p",
                LinkSelector = "a"
            },
            ["NtvSpor"] = new SiteConfig
            {
                BaseUrl = "https://www.ntvspor.net",
                SearchPath = "/ara?query={0}",
                ArticleSelector = "article",
                TitleSelector = "h2",
                ContentSelector = "p",
                LinkSelector = "a"
            }
        };

        // Hakem yorumcu isimleri (metin analizi için)
        private readonly List<string> _hakemYorumculari = new()
        {
            "Ahmet Çakar", "Erman Toroğlu", "Deniz Çoban", "Bünyamin Gezer",
            "Ömer Üründül", "Attila Gökçe", "Ali Ece"
        };

        // Hakem yorumu tespiti için anahtar kelimeler
        private readonly List<string> _hakemAnahtarKelimeler = new()
        {
            "hakem", "pozisyon", "penaltı", "ofsayt", "var", "faul",
            "kırmızı kart", "sarı kart", "tartışmalı", "hata", "karar", "yorum"
        };

        public HakemYorumuToplamaServisi(ApplicationDbContext context, 
            ILogger<HakemYorumuToplamaServisi> logger, 
            HttpClient httpClient,
            YouTubeScrapingService youtubeService,
            TVKanalScrapingService tvKanalServisi,
            // DI’dan eklendi
            PozisyonOtomatikTespitServisi pozisyonTespitServisi)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _youtubeService = youtubeService;
            _tvKanalService = tvKanalServisi;
            _pozisyonTespitServisi = pozisyonTespitServisi;
            
            // Türk spor siteleri ve scraping ayarları
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", 
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("DNT", "1");
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
            
            // Timeout ayarla
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> MacIcinYorumTopla(int macId)
        {
            var mac = await _context.Maclar.FirstOrDefaultAsync(m => m.Id == macId);
            
            // Skor kontrolünü sadece maç durumu için yap
            if (mac == null)
            {
                _logger.LogWarning($"Maç bulunamadı: ID {macId}");
                return false;
            }
            
            // Maç durumu kontrolü (skor kontrolü yerine)
            if (mac.Durum != MacDurumu.Bitti)
            {
                _logger.LogInformation("Maç henüz oynanmadı, yorum toplama atlanıyor: {EvSahibi} vs {Deplasman}", mac.EvSahibi, mac.Deplasman);
                return false;
            }
            
            // Skor kontrolü ekle
            if (mac?.Skor == "-" || string.IsNullOrEmpty(mac?.Skor))
            {
                _logger.LogInformation("Maç henüz oynanmadı, yorum toplama atlanıyor: {EvSahibi} vs {Deplasman}", mac?.EvSahibi, mac?.Deplasman);
                return false;
            }
            
            try
            {
                // Performanslı sorgu - AsSplitQuery() kullan
                mac = await _context.Maclar
                    .AsSplitQuery() // Çoklu koleksiyon include uyarısını çözer
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
                    var youtubeYorumlari = await _youtubeService!.MacIcinKanalYorumlariniTopla(macBilgisi, mac.MacTarihi, mac.Id); // mac.Id eklendi
                    bulunanYorumlar.AddRange(youtubeYorumlari);
                    _logger.LogInformation($"YouTube kanallarından {youtubeYorumlari.Count} yorum bulundu.");
                    
                    // YouTube'dan yorum geldiyse detaylı log
                    if (youtubeYorumlari.Any())
                    {
                        foreach (var yorum in youtubeYorumlari.Take(3)) // İlk 3 yorumu logla
                        {
                            _logger.LogInformation($"YouTube yorumu: {yorum.YorumcuAdi} - {yorum.Yorum?.Substring(0, Math.Min(100, yorum.Yorum?.Length ?? 0))}...");
                            // Null check eklendi
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
                    var tvYorumlari = await _tvKanalService.TumKanallarAra(macBilgisi, mac.Id); // mac.Id eklendi
                    bulunanYorumlar.AddRange(tvYorumlari);
                    _logger.LogInformation($"TV kanallarından {tvYorumlari?.Count ?? 0} yorum bulundu.");
                    // Null check eklendi
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
                        // Satır ~181'de
                        var siteYorumlari = await WebScrapingYap(site.Key, site.Value, macBilgisi, mac.MacTarihi, mac.Id);
                        gazeteYorumlari.AddRange(siteYorumlari);
                        _logger.LogInformation($"{site.Key} sitesinden {siteYorumlari.Count()} yorum bulundu.");
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
                        var baglantiYorumu = await BaglantiLinktenYorumCek(baglantiLink!, macId);
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

        private async Task<BulunanYorum?> YouTubeHtmlFallback(string youtubeUrl, string macBilgisi, int macId)
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
                    BulunmaTarihi = DateTime.Now,
                    MacId = macId  // ✅ EKLENDI!
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
                var yorum = await YouTubeHtmlFallback(youtubeUrl, macBilgisi, mac.Id);
                if (yorum == null)
                {
                    _logger.LogWarning($"YouTube linkinden yorum bulunamadı (API). HTML fallback denenecek: {youtubeUrl}");
                    yorum = await YouTubeHtmlFallback(youtubeUrl, macBilgisi, mac.Id);
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
                var pozisyonlar = await _youtubeService.TranscripttenPozisyonTespitEt(youtubeUrl, macBilgisi, mac.Id); // mac.Id eklendi
                
                if (!pozisyonlar.Any())
                {
                    _logger.LogWarning($"Transcript'ten pozisyon bulunamadı: {youtubeUrl}");
                    return false;
                }

                _logger.LogInformation($"Transcript'ten {pozisyonlar?.Count ?? 0} pozisyon bulundu, kaydediliyor...");
                // Null check eklendi
                
                _logger.LogInformation($"Transcript'ten {pozisyonlar?.Count ?? 0} pozisyon eklendi");
                // Null check eklendi
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

        private async Task<List<BulunanYorum>> WebScrapingYap(string siteKey, SiteConfig siteConfig, string aramaTerimi, DateTime macTarihi, int macId)
        {
            var bulunanYorumlar = new List<BulunanYorum>();
            
            try
            {
                var encodedTerm = Uri.EscapeDataString(aramaTerimi);
                var searchUrl = siteConfig.BaseUrl + string.Format(siteConfig.SearchPath, encodedTerm);
                
                _logger.LogInformation($"Web scraping: {searchUrl}");
                
                try
                {
                    var response = await _httpClient.GetStringAsync(searchUrl);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(response);
                    
                    // CSS selector'ı XPath'e çevir
                    var articleXPath = ConvertCssToXPath(siteConfig.ArticleSelector);
                    var articles = doc.DocumentNode.SelectNodes(articleXPath);
                    if (articles == null) 
                    {
                        _logger.LogWarning($"{siteKey} için makale elementleri bulunamadı. XPath: {articleXPath}");
                        return bulunanYorumlar;
                    }
                
                    foreach (var article in articles.Take(5)) // İlk 5 sonuç
                    {
                        try
                        {
                            var titleXPath = ConvertCssToXPath(siteConfig.TitleSelector);
                            var contentXPath = ConvertCssToXPath(siteConfig.ContentSelector);
                            var linkXPath = ConvertCssToXPath(siteConfig.LinkSelector);
                            var titleNode = article.SelectSingleNode(titleXPath);
                            var contentNode = article.SelectSingleNode(contentXPath);
                            var linkNode = article.SelectSingleNode(linkXPath);
                            
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
                                    MacId = macId, // YENİ: Maç ID'si eklendi
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
                        catch (Exception articleEx)
                        {
                            _logger.LogWarning(articleEx, $"{siteKey} makale işleme hatası: {articleEx.Message}");
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError(httpEx, $"{siteKey} HTTP isteği hatası: {httpEx.Message}");
                }
                catch (TaskCanceledException timeoutEx)
                {
                    _logger.LogError(timeoutEx, $"{siteKey} timeout hatası: {timeoutEx.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{siteKey} genel hata: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Web scraping hatası - Site: {siteKey}, Terim: {aramaTerimi}");
            }
            
            return bulunanYorumlar;
        }
        
        private async Task<BulunanYorum?> BaglantiLinktenYorumCek(string baglantiLink, int macId)
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
                    BulunmaTarihi = DateTime.Now,
                    MacId = macId  // ✅ EKLENDI!
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
            // Null/boş guard eklendi
            if (bulunanYorumlar == null || bulunanYorumlar.Count == 0)
            {
                return;
            }
        
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
            
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Video daha önce işlenmiş mi kontrol eder
        /// </summary>
        private async Task<bool> IsVideoAlreadyProcessed(string videoId, int macId)
        {
            return await _context.HakemYorumlari
                .AnyAsync(h => h.KaynakLink != null && 
                          h.KaynakLink.Contains(videoId) && 
                          h.Pozisyon.MacId == macId);
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
            
            return Random.Shared.Next(1, 90); // RASTGELE DAKİKA!
        }
        // Maç için otomatik pozisyon tespiti - AdminController ile uyumlu
        public async Task<string?> PozisyonlariOtomatikTespitEt(int macId)
        {
            try
            {
                var pozisyonlar = await _pozisyonTespitServisi.MacIcinPozisyonTespitEt(macId);
                if (pozisyonlar == null || !pozisyonlar.Any())
                {
                    _logger.LogInformation("Otomatik tespit: {MacId} için pozisyon bulunamadı.", macId);
                    return null;
                }

                var mevcutPozisyonlar = await _context.Pozisyonlar
                    .Where(p => p.MacId == macId)
                    .Select(p => new { p.Dakika, p.PozisyonTuru })
                    .ToListAsync();

                var eklenecekler = pozisyonlar
                    .Where(p => !mevcutPozisyonlar.Any(m => m.Dakika == p.Dakika && m.PozisyonTuru == p.PozisyonTuru))
                    .ToList();

                if (eklenecekler.Any())
                {
                    _context.Pozisyonlar.AddRange(eklenecekler);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Otomatik tespit: {MacId} için {Count} pozisyon kaydedildi.", macId, eklenecekler.Count);
                }

                var ilk = pozisyonlar.First();
                return $"{ilk.Dakika}. dakika: {ilk.PozisyonTuru} - {ilk.Aciklama}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Otomatik pozisyon tespiti sırasında hata: {MacId}", macId);
                return null;
            }
        }

        private string ConvertCssToXPath(string cssSelector)
        {
            if (string.IsNullOrEmpty(cssSelector))
                return "//*";
                
            // Basit CSS selector'ları XPath'e çevir
            var xpath = cssSelector
                .Replace(".", "//*[@class='")
                .Replace("#", "//*[@id='")
                .Replace(" ", "//")
                .Replace(">", "/");
            
            // Class selector'ları düzelt
            if (cssSelector.StartsWith("."))
            {
                var className = cssSelector.Substring(1);
                xpath = $"//*[contains(@class, '{className}')]";
            }
            // ID selector'ları düzelt
            else if (cssSelector.StartsWith("#"))
            {
                var idName = cssSelector.Substring(1);
                xpath = $"//*[@id='{idName}']";
            }
            // Tag selector'ları düzelt
            else if (!cssSelector.Contains(".") && !cssSelector.Contains("#"))
            {
                xpath = $"//{cssSelector}";
            }
            
            return xpath;
        }
    }

    public class BulunanYorum    {
        public int MacId { get; set; } // YENİ: Hangi maça ait olduğu bilgisi
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


