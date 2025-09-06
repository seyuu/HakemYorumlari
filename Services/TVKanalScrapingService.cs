using HtmlAgilityPack;
using HakemYorumlari.Models;
using System.Text.RegularExpressions;

namespace HakemYorumlari.Services
{
    public class TVKanalScrapingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TVKanalScrapingService> _logger;
        
        private readonly Dictionary<string, TVKanalConfig> _tvKanallari = new()
        {
            // beinsports için güncellenmiş seçiciler
            ["beinsports"] = new TVKanalConfig
            {
                BaseUrl = "https://www.beinsports.com.tr",
                SearchPath = "/arama?q={0}",
                VideoSelector = ".search-result-item, .video-card, .content-item, .video-item, .news-card, .article-card, [data-video], .media-item, .post-item",
                TitleSelector = ".title, .headline, h3, h4, .post-title, .article-title, .video-title, .news-title",
                LinkSelector = "a, .link, [href]"
            },
            ["trtspor"] = new TVKanalConfig
            {
                BaseUrl = "https://www.trtspor.com.tr",
                SearchPath = "/arama/{0}/1",
                VideoSelector = ".haber-item, .news-item, .video-card, .content-item, .article-card, .media-item, .post-item",
                TitleSelector = ".baslik, .title, .headline, h3, h4, .post-title, .article-title",
                LinkSelector = "a, .link, [href]"
            },
            ["aspor"] = new TVKanalConfig
            {
                BaseUrl = "https://www.aspor.com.tr",
                SearchPath = "/arama?query={0}",
                VideoSelector = ".video-card, .news-item, .content-item, .article-card, .media-item, .post-item, .haber-item",
                TitleSelector = ".title, .headline, h3, h4, .post-title, .article-title, .video-title, .baslik",
                LinkSelector = "a, .link, [href]"
            },
            ["fanatik"] = new TVKanalConfig
            {
                BaseUrl = "https://www.fanatik.com.tr",
                SearchPath = "/arama?q={0}",
                VideoSelector = ".news-item, .video-card, .content-item, .article-card, .media-item, .post-item, .haber-item",
                TitleSelector = ".title, .headline, h3, h4, .post-title, .article-title, .baslik",
                LinkSelector = "a, .link, [href]"
            },
            ["fotomac"] = new TVKanalConfig
            {
                BaseUrl = "https://www.fotomac.com.tr",
                SearchPath = "/arama?query={0}",
                VideoSelector = ".haber-item, .news-item, .video-card, .content-item, .article-card, .media-item, .post-item",
                TitleSelector = ".baslik, .title, .headline, h3, h4, .post-title, .article-title",
                LinkSelector = "a, .link, [href]"
            }
        };
        
        private readonly string[] _hakemAnahtarKelimeler = {
            "hakem", "penaltı", "VAR", "kırmızı kart", "sarı kart", "ofsayt",
            "faul", "tartışmalı pozisyon", "hatalı karar", "doğru karar",
            "hakem hatası", "VAR incelemesi", "pozisyon analizi"
        };
        
        private readonly Dictionary<string, string[]> _hakemYorumculari = new()
        {
            ["Erman Toroğlu"] = new[] { "Erman Toroğlu", "A Spor", "Beyaz TV" },
            ["Fırat Aydınus"] = new[] { "Fırat Aydınus", "beIN Sports", "Tivibu Spor" },
            ["Deniz Çoban"] = new[] { "Deniz Çoban", "TRT Spor", "Kanal D" },
            ["Bahattin Duran"] = new[] { "Bahattin Duran", "NTV Spor", "CNN Türk" },
            ["Bülent Yıldırım"] = new[] { "Bülent Yıldırım", "Habertürk TV", "Show TV" }
        };

        public TVKanalScrapingService(HttpClient httpClient, ILogger<TVKanalScrapingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            // Header ayarları...
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
            _httpClient.Timeout = TimeSpan.FromSeconds(15); // 30'dan 15'e düşürüldü
            
            // ❌ Constructor'da await kullanılamaz - kaldırıldı
            // await Task.Delay(1000);
        }
        
        public async Task<List<BulunanYorum>> TVKanalindanYorumAra(string kanalAdi, string macBilgisi, int macId)
        {
            if (!_tvKanallari.ContainsKey(kanalAdi)) 
            {
                _logger.LogWarning($"Desteklenmeyen kanal: {kanalAdi}");
                return new List<BulunanYorum>();
            }
            
            var kanal = _tvKanallari[kanalAdi];
            var yorumlar = new List<BulunanYorum>();
            
            try
            {
                var aramaTerimleri = GenerateAramaTerimleri(macBilgisi);
                
                foreach (var aramaTerimi in aramaTerimleri.Take(3)) // İlk 3 terimi kullan
                {
                    var searchUrl = kanal.BaseUrl + string.Format(kanal.SearchPath, Uri.EscapeDataString(aramaTerimi));
                    _logger.LogInformation($"{kanalAdi} araması: {aramaTerimi}");
                    
                    try
                    {
                        var response = await _httpClient.GetStringAsync(searchUrl);
                        var doc = new HtmlDocument();
                        doc.LoadHtml(response);
                        
                        // CSS selector'ı XPath'e çevir
                        var xpathSelector = ConvertCssToXPath(kanal.VideoSelector);
                        var videos = doc.DocumentNode.SelectNodes(xpathSelector);
                        
                        if (videos == null)
                        {
                            _logger.LogWarning($"{kanalAdi} için video elementleri bulunamadı. XPath: {xpathSelector}");
                            continue;
                        }
                    
                        foreach (var video in videos.Take(5))
                        {
                            try
                            {
                                var titleXPath = ConvertCssToXPath(kanal.TitleSelector);
                                var linkXPath = ConvertCssToXPath(kanal.LinkSelector);
                                var titleNode = video.SelectSingleNode(titleXPath);
                                var linkNode = video.SelectSingleNode(linkXPath);
                                
                                if (titleNode != null && IsHakemYorumuIceriyor(titleNode.InnerText))
                                {
                                    var link = linkNode?.GetAttributeValue("href", "");
                                    if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                                    {
                                        link = kanal.BaseUrl + link;
                                    }
                                    
                                    var yorum = new BulunanYorum
                                    {
                                        YorumcuAdi = ExtractYorumcu(titleNode.InnerText) ?? "TV Yorumcusu",
                                        Yorum = titleNode.InnerText.Trim(),
                                        DogruKarar = AnalyzeDogruKarar(titleNode.InnerText),
                                        KaynakLink = link,
                                        KaynakTuru = "TV_Web",
                                        Kanal = kanalAdi,
                                        BulunduguSite = kanalAdi,
                                        BulunmaTarihi = DateTime.Now,
                                        MacId = macId  // ✅ Artık çalışacak
                                    };
                                    
                                    yorumlar.Add(yorum);
                                }
                            }
                            catch (Exception videoEx)
                            {
                                _logger.LogWarning(videoEx, $"{kanalAdi} video işleme hatası: {videoEx.Message}");
                            }
                        }
                        
                        // Rate limiting
                        await Task.Delay(2000);
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _logger.LogError(httpEx, $"{kanalAdi} HTTP isteği hatası: {httpEx.Message}");
                    }
                    catch (TaskCanceledException timeoutEx)
                    {
                        _logger.LogError(timeoutEx, $"{kanalAdi} timeout hatası: {timeoutEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{kanalAdi} genel hata: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{kanalAdi} scraping hatası");
            }
            
            return yorumlar.DistinctBy(y => y.KaynakLink).ToList();
        }
        
        public async Task<List<BulunanYorum>> TumKanallarAra(string macBilgisi, int macId)
        {
            var tumYorumlar = new List<BulunanYorum>();
            
            var tasks = _tvKanallari.Keys.Select(async kanal =>
            {
                try
                {
                    return await TVKanalindanYorumAra(kanal, macBilgisi, macId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Kanal {kanal} arama hatası");
                    return new List<BulunanYorum>();
                }
            });
            
            var results = await Task.WhenAll(tasks);
            
            foreach (var result in results)
            {
                tumYorumlar.AddRange(result);
            }
            
            return tumYorumlar.DistinctBy(y => y.KaynakLink).ToList();
        }
        
        private List<string> GenerateAramaTerimleri(string macBilgisi)
        {
            return new List<string>
            {
                $"{macBilgisi} hakem yorumu",
                $"{macBilgisi} tartışmalı pozisyon",
                $"{macBilgisi} VAR",
                $"{macBilgisi} penaltı",
                $"{macBilgisi} analiz"
            };
        }
        
        private bool IsHakemYorumuIceriyor(string text)
        {
            var lowerText = text.ToLower();
            return _hakemAnahtarKelimeler.Any(kelime => lowerText.Contains(kelime.ToLower()));
        }
        
        private string? ExtractYorumcu(string text)
        {
            return _hakemYorumculari.Keys.FirstOrDefault(yorumcu => 
                text.Contains(yorumcu, StringComparison.OrdinalIgnoreCase));
        }
        
        private bool AnalyzeDogruKarar(string text)
        {
            var lowerText = text.ToLower();
            var dogruKelimeler = new[] { "doğru", "haklı", "yerinde", "uygun", "isabetli" };
            var yanlisKelimeler = new[] { "yanlış", "hatalı", "uygunsuz", "isabetsiz", "tartışmalı" };
            
            var dogruSayisi = dogruKelimeler.Count(kelime => lowerText.Contains(kelime));
            var yanlisSayisi = yanlisKelimeler.Count(kelime => lowerText.Contains(kelime));
            
            return dogruSayisi >= yanlisSayisi;
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
    
    public class TVKanalConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string SearchPath { get; set; } = string.Empty;
        public string VideoSelector { get; set; } = string.Empty;
        public string TitleSelector { get; set; } = string.Empty;
        public string LinkSelector { get; set; } = string.Empty;
        public string? EmbedPattern { get; set; }
    }
}