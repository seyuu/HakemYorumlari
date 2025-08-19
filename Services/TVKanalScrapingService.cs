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
            ["beinsports"] = new TVKanalConfig
            {
                BaseUrl = "https://www.beinsports.com.tr",
                SearchPath = "/arama?q={0}",
                VideoSelector = ".video-item",
                TitleSelector = ".video-title",
                LinkSelector = "a",
                EmbedPattern = "https://embed.beinsports.com.tr/player/{0}"
            },
            ["trtspor"] = new TVKanalConfig
            {
                BaseUrl = "https://www.trtspor.com.tr",
                SearchPath = "/arama/{0}",
                VideoSelector = ".haber-item",
                TitleSelector = ".baslik",
                LinkSelector = "a"
            },
            ["aspor"] = new TVKanalConfig
            {
                BaseUrl = "https://www.aspor.com.tr",
                SearchPath = "/arama?q={0}",
                VideoSelector = ".video-card",
                TitleSelector = ".title",
                LinkSelector = "a"
            },
            ["fanatik"] = new TVKanalConfig
            {
                BaseUrl = "https://www.fanatik.com.tr",
                SearchPath = "/arama?q={0}",
                VideoSelector = ".news-item",
                TitleSelector = ".title",
                LinkSelector = "a"
            },
            ["fotomac"] = new TVKanalConfig
            {
                BaseUrl = "https://www.fotomac.com.tr",
                SearchPath = "/arama/{0}",
                VideoSelector = ".haber-item",
                TitleSelector = ".baslik",
                LinkSelector = "a"
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
            
            // User-Agent ayarla
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }
        
        public async Task<List<BulunanYorum>> TVKanalindanYorumAra(string kanalAdi, string macBilgisi)
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
                    
                    var response = await _httpClient.GetStringAsync(searchUrl);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(response);
                    
                    var videos = doc.DocumentNode.SelectNodes(kanal.VideoSelector);
                    
                    if (videos != null)
                    {
                        foreach (var video in videos.Take(5))
                        {
                            var titleNode = video.SelectSingleNode(kanal.TitleSelector);
                            var linkNode = video.SelectSingleNode(kanal.LinkSelector);
                            
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
                                    BulunmaTarihi = DateTime.Now
                                };
                                
                                yorumlar.Add(yorum);
                            }
                        }
                    }
                    
                    // Rate limiting
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{kanalAdi} scraping hatası");
            }
            
            return yorumlar.DistinctBy(y => y.KaynakLink).ToList();
        }
        
        public async Task<List<BulunanYorum>> TumKanallarAra(string macBilgisi)
        {
            var tumYorumlar = new List<BulunanYorum>();
            
            var tasks = _tvKanallari.Keys.Select(async kanal =>
            {
                try
                {
                    return await TVKanalindanYorumAra(kanal, macBilgisi);
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