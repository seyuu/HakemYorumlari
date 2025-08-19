using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using HakemYorumlari.Models;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Net;
using System.Xml.Linq;
using System.Globalization;
using System.Text;

namespace HakemYorumlari.Services
{
    public class YouTubeScrapingService : IDisposable
    {
        private readonly ILogger<YouTubeScrapingService> _logger;
        private YouTubeService? _youtubeService; // readonly kaldırıldı
        private readonly string? _apiKey;
        private readonly HttpClient _httpClient;
        private readonly bool _useOAuth2;
        private readonly string? _clientSecretPath;

        // Direkt YouTube kanalları ve playlist'leri
        private readonly Dictionary<string, KanalBilgisi> _sporKanallari = new()
        {
            ["Ekol TV - Erman Toroğlu"] = new KanalBilgisi 
            { 
                KanalId = "UCccxXUKSuqOrlWQxweZBAQw",
                PlaylistId = "PLslvT4XP0_nKowGC1sL5A6Ft5wRf4lsz7",
                YorumcuAdi = "Erman Toroğlu",
                KanalTuru = "TV Kanalı"
            },
            ["A Spor - Mustafa Çulcu"] = new KanalBilgisi 
            { 
                KanalId = "UCJElRTCNEmLemgirqvsW63Q",
                YorumcuAdi = "Mustafa Çulcu",
                KanalTuru = "TV Kanalı"
            },
            ["TRT Spor - Bünyamin Gezer"] = new KanalBilgisi 
            { 
                KanalId = "UCfYNqluOf8EbQkL44otydMw",
                PlaylistId = "PLA_JQAuwYFsQxIDQHYaV27NG59ch6oSQe",
                YorumcuAdi = "Bünyamin Gezer",
                KanalTuru = "TV Kanalı"
            },
            ["beIN Sports - Trio"] = new KanalBilgisi 
            { 
                KanalId = "UCPe9vNjHF1kEExT5kHwc7aw",
                PlaylistId = "PLREq_OnJpFaQcEpAY7KIOrAaJ39QX9SHg",
                YorumcuAdi = "beIN Sports Trio",
                KanalTuru = "TV Kanalı"
            }
        };

        // Hakemle ilgili kelimeler (genel/gürültülü olanları çıkardık: video, gol, tekrar vb.)
        private readonly List<string> _hakemAnahtarKelimeler = new()
        {
            "hakem", "var", "penaltı", "penalti", "kırmızı kart", "kirmizi kart",
            "sarı kart", "sari kart", "ofsayt", "offside", "faul", "pozisyon",
            "karar", "tartışmalı", "tartismali", "hata", "doğru karar", "yanlış karar"
        };

        public class KanalBilgisi
        {
            public string KanalId { get; set; } = "";
            public string? PlaylistId { get; set; }
            public string YorumcuAdi { get; set; } = "";
            public string KanalTuru { get; set; } = "";
        }

        public YouTubeScrapingService(ILogger<YouTubeScrapingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _logger.LogInformation("YouTubeScrapingService constructor başlatıldı");
            
            _apiKey = configuration["YouTube:ApiKey"];
            _useOAuth2 = configuration.GetValue<bool>("YouTube:UseOAuth2", false);
            _clientSecretPath = configuration["YouTube:ClientSecretPath"];
            
            _logger.LogInformation($"YouTube API Key alındı: {(!string.IsNullOrEmpty(_apiKey) ? "VAR" : "YOK")}");
            _logger.LogInformation($"OAuth2 kullanımı: {_useOAuth2}");
            
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HakemYorumlari/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://hakemyorumlari.com.tr");
            _httpClient.DefaultRequestHeaders.Add("Origin", "https://hakemyorumlari.com.tr");

            if (_useOAuth2 && !string.IsNullOrEmpty(_clientSecretPath))
            {
                _ = Task.Run(InitializeOAuth2ServiceAsync);
            }
            else if (!string.IsNullOrEmpty(_apiKey))
            {
                InitializeApiKeyService();
            }
            else
            {
                _logger.LogWarning("Ne OAuth2 ne de API anahtarı bulunamadı. YouTube servisi devre dışı.");
            }
        }

        // ------------------------ Yardımcılar (TR normalize / tam kelime arama / takım ayrıştırma) ------------------------
        private static string NormalizeTr(string s)
        {
            var lower = s.ToLower(new CultureInfo("tr-TR"));
            var formD = lower.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool ContainsWholeWord(string text, string term) =>
            Regex.IsMatch(text, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static (string? Home, string? Away) ParseTeams(string macBilgisi)
        {
            if (string.IsNullOrWhiteSpace(macBilgisi)) return (null, null);
            
            // Farklı formatları destekle
            var s = macBilgisi
                .Replace(" vs ", " - ", StringComparison.OrdinalIgnoreCase)
                .Replace(" - ", " - ", StringComparison.OrdinalIgnoreCase)
                .Replace("  ", " ", StringComparison.OrdinalIgnoreCase); // Çift boşlukları tek yap
            
            // Önce " - " ile böl
            var parts = s.Split(new[] { '-', '–', '—' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .ToArray();
            
            if (parts.Length == 2) return (parts[0], parts[1]);
            
            // Eğer " - " ile bölünemezse, boşluklarla böl ve son 2'yi al
            var spaceParts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (spaceParts.Length >= 2)
            {
                var home = string.Join(" ", spaceParts.Take(spaceParts.Length - 1));
                var away = spaceParts.Last();
                return (home, away);
            }
            
            return (null, null);
        }

        /// <summary>
        /// Maç için direkt kanallardan yorum toplar
        /// </summary>
        public async Task<List<BulunanYorum>> MacIcinKanalYorumlariniTopla(string macBilgisi, DateTime macTarihi)
        {
            _logger.LogInformation($"MacIcinKanalYorumlariniTopla başlatıldı: MacBilgisi={macBilgisi}, Tarih={macTarihi:dd.MM.yyyy}");
            
            if (_youtubeService == null)
            {
                _logger.LogError("YouTube servisi NULL! Başlatılamadı.");
                return new List<BulunanYorum>();
            }
            
            _logger.LogInformation("YouTube servisi mevcut, kanal tarama başlıyor...");

            var bulunanYorumlar = new List<BulunanYorum>();

            try
            {
                foreach (var kanal in _sporKanallari)
                {
                    _logger.LogInformation($"Kanal taranıyor: {kanal.Key}");
                    
                    try
                    {
                        var kanalYorumlari = await KanaldanYorumTopla(kanal.Value, macBilgisi, macTarihi);
                        bulunanYorumlar.AddRange(kanalYorumlari);
                        
                        _logger.LogInformation($"{kanal.Key} kanalından {kanalYorumlari.Count} yorum bulundu");
                        
                        // Rate limiting - YouTube API günlük limiti var
                        await Task.Delay(2000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{kanal.Key} kanalından yorum toplama hatası");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kanal yorum toplama genel hatası");
            }

            return bulunanYorumlar;
        }

        /// <summary>
        /// Belirli bir kanaldan yorum toplar
        /// </summary>
        private async Task InitializeOAuth2ServiceAsync()
        {
            try
            {
                _logger.LogInformation("OAuth2 authentication başlatılıyor...");
                
                UserCredential credential;
                using (var stream = new FileStream(_clientSecretPath!, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromStream(stream).Secrets,
            new[] { YouTubeService.Scope.YoutubeReadonly },
            "user",
            CancellationToken.None,
            new FileDataStore("HakemYorumlari.OAuth2", true),
            new LocalServerCodeReceiver());
                }

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "HakemYorumlari"
                });
                
                // Thread-safe assignment
                Interlocked.Exchange(ref _youtubeService, youtubeService);
                
                _logger.LogInformation("OAuth2 YouTube servisi başarıyla başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OAuth2 YouTube servisi başlatılırken hata oluştu");
                // Fallback to API key if available
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogInformation("OAuth2 başarısız, API key ile deneniyor...");
                    InitializeApiKeyService();
                }
            }
        }

        private void InitializeApiKeyService()
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = _apiKey,
                    ApplicationName = "HakemYorumlari"
                });
                
                // Referer ve header'lar ekle
                youtubeService.HttpClient.DefaultRequestHeaders.Add("User-Agent", "HakemYorumlari/1.0");
                youtubeService.HttpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                youtubeService.HttpClient.DefaultRequestHeaders.Add("Referer", "https://hakemyorumlari.com.tr");
                youtubeService.HttpClient.DefaultRequestHeaders.Add("Origin", "https://hakemyorumlari.com.tr");
                
                // Thread-safe assignment
                Interlocked.Exchange(ref _youtubeService, youtubeService);
                
                _logger.LogInformation("API Key ile YouTube servisi başarıyla başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API Key ile YouTube servisi başlatılırken hata oluştu");
            }
        }

        private async Task<List<BulunanYorum>> KanaldanYorumTopla(KanalBilgisi kanal, string macBilgisi, DateTime macTarihi)
        {
            var yorumlar = new List<BulunanYorum>();

            try
            {
                // 1. Playlist'ten video çek (eğer playlist varsa)
                if (!string.IsNullOrEmpty(kanal.PlaylistId))
                {
                    var playlistVideolari = await PlaylistVideolariniGetir(kanal.PlaylistId, macTarihi);
                    foreach (var video in playlistVideolari)
                    {
                        if (IsMacIleIlgiliVideo(video.Snippet.Title, video.Snippet.Description, macBilgisi))
                        {
                            var yorum = await VideoDetaylariniAl(video, macBilgisi, kanal);
                            if (yorum != null)
                            {
                                yorumlar.Add(yorum);
                            }
                        }
                    }
                }

                // 2. Kanal ID'den video çek
                var kanalVideolari = await KanalVideolariniGetir(kanal.KanalId, macTarihi, macBilgisi);
                foreach (var video in kanalVideolari)
                {
                    if (IsMacIleIlgiliVideo(video.Snippet.Title, video.Snippet.Description, macBilgisi))
                    {
                        var yorum = await VideoDetaylariniAl(video, macBilgisi, kanal);
                        if (yorum != null)
                        {
                            yorumlar.Add(yorum);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{kanal.YorumcuAdi} kanalından yorum toplama hatası");
            }

            return yorumlar;
        }

        /// <summary>
        /// Playlist'ten videoları getirir (sayfalama + tarih filtresi)
        /// </summary>
        private async Task<List<PlaylistItem>> PlaylistVideolariniGetir(string playlistId, DateTime macTarihi)
        {
            try
            {
                var result = new List<PlaylistItem>();
                var req = _youtubeService!.PlaylistItems.List("snippet");
                req.PlaylistId = playlistId;
                req.MaxResults = 50;
                // kota için partial response
                req.Fields = "items(snippet/resourceId/videoId,snippet/publishedAt,snippet/title,snippet/description),nextPageToken";

                var after = macTarihi.AddDays(-5);
                var before = macTarihi.AddDays(5);

                string? page = null;
                do
                {
                    req.PageToken = page;
                    var resp = await req.ExecuteAsync();
                    foreach (var item in resp.Items ?? Enumerable.Empty<PlaylistItem>())
                    {
                        if (item.Snippet?.PublishedAtDateTimeOffset != null)
                        {
                            var dt = item.Snippet.PublishedAtDateTimeOffset.Value.LocalDateTime;
                            if (dt >= after && dt <= before)
                                result.Add(item);
                        }
                    }
                    page = resp.NextPageToken;
                } while (!string.IsNullOrEmpty(page));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Playlist videoları getirme hatası: {playlistId}");
                return new List<PlaylistItem>();
            }
        }

        /// <summary>
        /// Kanal ID'den videoları getirir (sayfalama + tarih filtresi + partial fields)
        /// </summary>
        private async Task<List<SearchResult>> KanalVideolariniGetir(string kanalId, DateTime macTarihi, string macBilgisi)
        {
            try
            {
                var result = new List<SearchResult>();
                var req = _youtubeService!.Search.List("snippet");
                req.ChannelId = kanalId;
                req.Type = "video";
                req.MaxResults = 50;
                req.Order = SearchResource.ListRequest.OrderEnum.Date;
                req.PublishedAfterDateTimeOffset = macTarihi.AddDays(-5);
                req.PublishedBeforeDateTimeOffset = macTarihi.AddDays(5);
                // kota için partial response
                req.Fields = "items(id/videoId,snippet/title,snippet/description),nextPageToken";

                // --- Bonus: takım adları + hakem kelimeleri ile sorgu daraltma ---
                var (home, away) = ParseTeams(macBilgisi); // ✅ Düzeltildi: macBilgisi parametresi
                var qParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(home)) qParts.Add(home);
                if (!string.IsNullOrWhiteSpace(away)) qParts.Add(away);
                qParts.Add("(hakem OR var OR penaltı OR ofsayt OR \"kırmızı kart\" OR \"sarı kart\")");
                req.Q = string.Join(" ", qParts);
                // ----------------------------------------------------------------

                string? page = null;
                do
                {
                    req.PageToken = page;
                    var resp = await req.ExecuteAsync();
                    if (resp.Items != null) result.AddRange(resp.Items);
                    page = resp.NextPageToken;
                } while (!string.IsNullOrEmpty(page));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Kanal videoları getirme hatası: {kanalId}");
                return new List<SearchResult>();
            }
        }

        /// <summary>
        /// Video maç ile ilgili mi kontrol eder
        /// </summary>
        private bool IsMacIleIlgiliVideo(string? title, string? description, string macBilgisi)
        {
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description)) return false;

            var full = (title ?? "") + " " + (description ?? "");
            var nText = NormalizeTr(full);
            var (home, away) = ParseTeams(macBilgisi);

            // Takım adları var mı kontrol et
            var hasTeam = false;
            if (!string.IsNullOrWhiteSpace(home)) 
            {
                hasTeam |= ContainsWholeWord(nText, NormalizeTr(home));
                hasTeam |= nText.Contains(NormalizeTr(home)); // Tam kelime olmadan da kontrol et
            }
            if (!string.IsNullOrWhiteSpace(away)) 
            {
                hasTeam |= ContainsWholeWord(nText, NormalizeTr(away));
                hasTeam |= nText.Contains(NormalizeTr(away)); // Tam kelime olmadan da kontrol et
            }

            // Hakem kelimeleri var mı kontrol et
            var hakemKelimeVarMi = _hakemAnahtarKelimeler.Any(k => nText.Contains(NormalizeTr(k)));

            // 1. Takım adı + hakem kelimesi → Kesin ilgili
            if (hasTeam && hakemKelimeVarMi) return true;

            // 2. Sadece hakem kelimesi + yorumcu adı → Muhtemelen ilgili
            if (hakemKelimeVarMi && _sporKanallari.Values.Any(k => nText.Contains(NormalizeTr(k.YorumcuAdi))))
                return true;

            // 3. Sadece takım adı → Maç ile ilgili olabilir
            if (hasTeam) return true;

            // 4. Sadece hakem kelimesi → Genel hakem yorumu olabilir
            if (hakemKelimeVarMi) return true;

            return false;
        }

        /// <summary>
        /// Video detaylarını alır ve yorum oluşturur
        /// </summary>
        private async Task<BulunanYorum?> VideoDetaylariniAl(PlaylistItem video, string macBilgisi, KanalBilgisi kanal)
        {
            try
            {
                var videoRequest = _youtubeService!.Videos.List("snippet,statistics");
                videoRequest.Id = video.Snippet.ResourceId.VideoId;
                // kota için partial response
                videoRequest.Fields = "items(id,snippet/title,snippet/description,statistics/viewCount)";

                var videoResponse = await videoRequest.ExecuteAsync();
                var videoDetay = videoResponse.Items?.FirstOrDefault();

                if (videoDetay == null) return null;

                // Transcript'ten pozisyon tespit et
                var videoUrl = $"https://www.youtube.com/watch?v={video.Snippet.ResourceId.VideoId}";
                var pozisyonlar = await TranscripttenPozisyonTespitEt(videoUrl, macBilgisi);

                if (pozisyonlar.Count > 0)
                {
                    // İlk pozisyonu döndür
                    var ilkPozisyon = pozisyonlar.First();
                    ilkPozisyon.YorumcuAdi = kanal.YorumcuAdi;
                    ilkPozisyon.Kanal = kanal.KanalTuru;
                    return ilkPozisyon;
                }

                // Pozisyon bulunamazsa video başlığını döndür
                return new BulunanYorum
                {
                    YorumcuAdi = kanal.YorumcuAdi,
                    Yorum = videoDetay.Snippet.Title,
                    DogruKarar = false,
                    Kanal = kanal.KanalTuru,
                    KaynakLink = videoUrl,
                    KaynakTuru = "YouTube",
                    BulunduguSite = "YouTube",
                    BulunmaTarihi = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Video detayları alma hatası: {video.Snippet.ResourceId.VideoId}");
                return null;
            }
        }

        /// <summary>
        /// Video detaylarını alır ve yorum oluşturur (SearchResult için)
        /// </summary>
        private async Task<BulunanYorum?> VideoDetaylariniAl(SearchResult video, string macBilgisi, KanalBilgisi kanal)
        {
            try
            {
                var videoRequest = _youtubeService!.Videos.List("snippet,statistics");
                videoRequest.Id = video.Id.VideoId;
                // kota için partial response
                videoRequest.Fields = "items(id,snippet/title,snippet/description,statistics/viewCount)";

                var videoResponse = await videoRequest.ExecuteAsync();
                var videoDetay = videoResponse.Items?.FirstOrDefault();

                if (videoDetay == null) return null;

                // Transcript'ten pozisyon tespit et
                var videoUrl = $"https://www.youtube.com/watch?v={video.Id.VideoId}";
                var pozisyonlar = await TranscripttenPozisyonTespitEt(videoUrl, macBilgisi);

                if (pozisyonlar.Count > 0)
                {
                    // İlk pozisyonu döndür
                    var ilkPozisyon = pozisyonlar.First();
                    ilkPozisyon.YorumcuAdi = kanal.YorumcuAdi;
                    ilkPozisyon.Kanal = kanal.KanalTuru;
                    return ilkPozisyon;
                }

                // Pozisyon bulunamazsa video başlığını döndür
                return new BulunanYorum
                {
                    YorumcuAdi = kanal.YorumcuAdi,
                    Yorum = videoDetay.Snippet.Title,
                    DogruKarar = false,
                    Kanal = kanal.KanalTuru,
                    KaynakLink = videoUrl,
                    KaynakTuru = "YouTube",
                    BulunduguSite = "YouTube",
                    BulunmaTarihi = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Video detayları alma hatası: {video.Id.VideoId}");
                return null;
            }
        }

        private string? ParseVideoIdFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var v = query.Get("v");
                if (!string.IsNullOrEmpty(v)) return v;
                // youtu.be/VIDEOID formatı
                if (uri.Host.Contains("youtu.be"))
                {
                    var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                    return segments.FirstOrDefault();
                }
                // /shorts/VIDEOID veya /live/VIDEOID
                var path = uri.AbsolutePath.Trim('/');
                if (path.StartsWith("shorts/") || path.StartsWith("live/"))
                {
                    var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    return parts.Length > 1 ? parts[1] : null;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<BulunanYorum?> VideoLinkindenTekYorum(string youtubeUrl, string macBilgisi, string? yorumcuAdiOverride = null)
        {
            if (_youtubeService == null) 
            {
                _logger.LogWarning("YouTube servisi null - API key eksik olabilir");
                return null;
            }

            _logger.LogInformation($"VideoLinkindenTekYorum başlatıldı: URL={youtubeUrl}, MacBilgisi={macBilgisi}");

            var videoId = ParseVideoIdFromUrl(youtubeUrl);
            if (string.IsNullOrEmpty(videoId))
            {
                _logger.LogWarning($"YouTube video ID parse edilemedi: {youtubeUrl}");
                return null;
            }

            _logger.LogInformation($"Video ID parse edildi: {videoId}");

            try
            {
                var videoRequest = _youtubeService.Videos.List("snippet,statistics");
                videoRequest.Id = videoId;
                var response = await videoRequest.ExecuteAsync();
                var video = response.Items?.FirstOrDefault();
                
                if (video == null) 
                {
                    _logger.LogWarning($"YouTube API'den video bulunamadı: {videoId}");
                    return null;
                }

                _logger.LogInformation($"Video bulundu: Title={video.Snippet?.Title}, Channel={video.Snippet?.ChannelTitle}");

                var title = video.Snippet?.Title ?? "";
                var desc = video.Snippet?.Description ?? "";
                
                var isMacIleIlgili = IsMacIleIlgiliVideo(title, desc, macBilgisi);
                _logger.LogInformation($"Video maçla ilişkili mi: {isMacIleIlgili}");

                if (!isMacIleIlgili)
                {
                    _logger.LogInformation($"Video maçla ilişkili değil görünüyor: {title}");
                    // Yine de yorum olarak ekle - kullanıcı manuel olarak eklemek istiyor
                }

                var yorum = new BulunanYorum
                {
                    YorumcuAdi = !string.IsNullOrWhiteSpace(yorumcuAdiOverride) ? yorumcuAdiOverride : (video.Snippet?.ChannelTitle ?? "YouTube"),
                    Yorum = title + (string.IsNullOrWhiteSpace(desc) ? "" : (" - " + desc)),
                    DogruKarar = false, // TODO: bool? ise null
                    Kanal = video.Snippet?.ChannelTitle ?? "YouTube",
                    KaynakLink = youtubeUrl,
                    KaynakTuru = "YouTube",
                    BulunduguSite = "YouTube",
                    BulunmaTarihi = DateTime.Now
                };

                _logger.LogInformation($"BulunanYorum oluşturuldu: {yorum.YorumcuAdi} - {yorum.Yorum.Substring(0, Math.Min(50, yorum.Yorum.Length))}...");
                return yorum;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"VideoLinkindenTekYorum hata: {youtubeUrl}");
                return null;
            }
        }

        /// <summary>
        /// YouTube transcript'ten pozisyonları tespit eder (HTML scraping ile)
        /// </summary>
        public async Task<List<BulunanYorum>> TranscripttenPozisyonTespitEt(string youtubeUrl, string macBilgisi)
        {
            try
            {
                var videoId = ParseVideoIdFromUrl(youtubeUrl);
                if (string.IsNullOrEmpty(videoId))
                {
                    _logger.LogWarning($"Video ID parse edilemedi: {youtubeUrl}");
                    return new List<BulunanYorum>();
                }

                _logger.LogInformation($"Transcript pozisyon tespiti başlatıldı: {videoId}");

                // Önce HTML scraping ile dene (en güvenilir)
                var transcript = await GetTranscriptViaHTMLScraping(videoId);
                List<(TimeSpan Offset, string Text)> segments = new();
                if (transcript != null && transcript.Count > 0)
                {
                    segments = transcript;
                    _logger.LogInformation($"HTML scraping üzerinden {segments.Count} transcript segmenti alındı: {videoId}");
                }
                else
                {
                    // HTML scraping çalışmazsa TimedText ile dene
                    if (transcript == null || transcript.Count == 0)
                    {
                        transcript = await GetTranscriptViaTimedText(videoId);
                    }

                    // Son çare olarak HTML fallback
                    if (transcript == null || transcript.Count == 0)
                    {
                        var transcriptRaw = await GetTranscriptFromHtml(videoId);

                        if (!string.IsNullOrWhiteSpace(transcriptRaw))
                        {
                            // Basit satır-parçalama; zaman etiketlerini ayıkla
                            foreach (var satir in transcriptRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            {
                                var s = satir.Trim();
                                if (string.IsNullOrEmpty(s)) continue;
                                var m = Regex.Match(s, @"(\d{1,2}):(\d{2})");
                                var ts = TimeSpan.Zero;
                                if (m.Success)
                                {
                                    ts = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
                                }
                                segments.Add((ts, s));
                            }
                            _logger.LogInformation($"HTML üzerinden {segments.Count} transcript satırı alındı: {videoId}");
                        }
                    }
                }

                if (segments.Count == 0)
                {
                    _logger.LogWarning($"Transcript bulunamadı: {videoId}");
                    return new List<BulunanYorum>();
                }

                var pozisyonlar = new List<BulunanYorum>();
                var pozisyonTespitEdildi = new HashSet<string>();

                foreach (var segment in segments)
                {
                    var dakika = (int)segment.Offset.TotalMinutes;
                    var metin = NormalizeTr(segment.Text);
                    var pozisyonTuru = PozisyonTuruTespitEt(metin);
                    if (string.IsNullOrEmpty(pozisyonTuru)) continue;

                    var key = $"{dakika}_{pozisyonTuru}";
                    if (pozisyonTespitEdildi.Contains(key)) continue;
                    pozisyonTespitEdildi.Add(key);

                    pozisyonlar.Add(new BulunanYorum
                    {
                        YorumcuAdi = "Transcript Tespit",
                        Yorum = $"[{segment.Offset.Minutes:D2}:{segment.Offset.Seconds:D2}] {pozisyonTuru}: {segment.Text}",
                        DogruKarar = false,
                        Kanal = "YouTube Transcript",
                        KaynakLink = youtubeUrl,
                        KaynakTuru = "YouTube",
                        BulunduguSite = "YouTube",
                        BulunmaTarihi = DateTime.Now
                    });
                }

                _logger.LogInformation($"Transcript'ten toplam {pozisyonlar.Count} pozisyon tespit edildi");
                return pozisyonlar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Transcript pozisyon tespiti hatası: {youtubeUrl}");
                return new List<BulunanYorum>();
            }
        }

        /// <summary>
        /// YouTube video sayfasından transcript'i HTML scraping ile çeker
        /// </summary>
        private async Task<string?> GetTranscriptFromHtml(string videoId)
        {
            try
            {
                var url = $"https://www.youtube.com/watch?v={videoId}";
                var html = await _httpClient.GetStringAsync(url);
                
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // 1. Transcript butonunu farklı yöntemlerle bul
                var transcriptButton = doc.DocumentNode.SelectSingleNode("//button[contains(@aria-label, 'transcript') or contains(@aria-label, 'Transcript') or contains(@aria-label, 'altyazı') or contains(@aria-label, 'Altyazı')]");
                
                if (transcriptButton == null)
                {
                    // 2. Transcript link'ini bul
                    transcriptButton = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'transcript') or contains(@href, 'cc')]");
                }
                
                if (transcriptButton == null)
                {
                    // 3. Transcript panel'ini direkt bul
                    var transcriptPanel = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'ytd-transcript-segment-renderer') or contains(@class, 'transcript') or contains(@class, 'cc')]");
                    if (transcriptPanel != null)
                    {
                        var panelText = transcriptPanel.InnerText;
                        _logger.LogInformation($"Transcript panel bulundu: {videoId}");
                        return panelText;
                    }
                }

                if (transcriptButton == null)
                {
                    // 4. Video açıklamasında transcript var mı kontrol et
                    var description = doc.DocumentNode.SelectSingleNode("//div[@id='description']//yt-formatted-string");
                    if (description != null)
                    {
                        var descText = description.InnerText;
                        if (descText.Contains("transcript") || descText.Contains("altyazı") || descText.Contains("cc"))
                        {
                            _logger.LogInformation($"Video açıklamasında transcript bilgisi bulundu: {videoId}");
                            return descText;
                        }
                    }

                    // 5. Sayfa içeriğinde transcript kelimesi var mı ara
                    var pageContent = doc.DocumentNode.InnerText;
                    if (pageContent.Contains("transcript") || pageContent.Contains("altyazı") || pageContent.Contains("cc"))
                    {
                        _logger.LogInformation($"Sayfa içeriğinde transcript bilgisi bulundu: {videoId}");
                        return pageContent;
                    }

                    _logger.LogWarning($"Transcript bulunamadı: {videoId}");
                    return null;
                }

                // Transcript butonu bulundu, şimdi panel'i bul
                var transcriptContainer = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'ytd-transcript-segment-renderer') or contains(@class, 'transcript') or contains(@class, 'cc') or contains(@id, 'transcript')]");
                
                if (transcriptContainer == null)
                {
                    // Transcript butonu var ama panel yok, sayfa yeniden yüklenmeli
                    _logger.LogWarning($"Transcript butonu bulundu ama panel bulunamadı: {videoId}");
                    return null;
                }

                var containerText = transcriptContainer.InnerText;
                _logger.LogInformation($"Transcript başarıyla çekildi: {videoId}");
                return containerText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"HTML transcript çekme hatası: {videoId}");
                return null;
            }
        }

/// <summary>
/// Resmi timedtext endpoint üzerinden transcript segmentlerini çeker - Sadece Türkçe
/// </summary>
private async Task<List<(TimeSpan Offset, string Text)>?> GetTranscriptViaTimedText(string videoId)
{
    try
    {
        // Sadece Türkçe transcript endpoint'leri
        var candidates = new List<string>
        {
            // Türkçe transcript'ler - farklı format ve kaynak kombinasyonları
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&kind=asr&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr-TR&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr-TR&kind=asr&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&fmt=srv2",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&kind=asr&fmt=srv2",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr-TR&fmt=srv2",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr-TR&kind=asr&fmt=srv2",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&fmt=srv1",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&kind=asr&fmt=srv1",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&kind=asr",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr-TR",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr-TR&kind=asr",
            
            // Google Video API - Türkçe alternatif endpoint'ler
            $"https://video.google.com/timedtext?v={videoId}&lang=tr&fmt=srv3",
            $"https://video.google.com/timedtext?v={videoId}&lang=tr&kind=asr&fmt=srv3",
            $"https://video.google.com/timedtext?v={videoId}&lang=tr-TR&fmt=srv3",
            $"https://video.google.com/timedtext?v={videoId}&lang=tr-TR&kind=asr&fmt=srv3",
            $"https://video.google.com/timedtext?v={videoId}&lang=tr&fmt=srv2",
            $"https://video.google.com/timedtext?v={videoId}&lang=tr&kind=asr&fmt=srv2",
            $"https://video.google.com/timedtext?v={videoId}&lang=tr",
            $"https://video.google.com/timedtext?v={videoId}&lang=tr&kind=asr"
        };

        _logger.LogInformation($"Video {videoId} için {candidates.Count} Türkçe TimedText endpoint'i deneniyor...");

        for (int i = 0; i < candidates.Count; i++)
        {
            var url = candidates[i];
            try
            {
                _logger.LogDebug($"Video {videoId} için Türkçe TimedText endpoint {i+1}/{candidates.Count}: {url}");
                
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Türk kullanıcısı header'ları
                req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                req.Headers.Add("Referer", "https://www.youtube.com/");
                req.Headers.Add("Origin", "https://www.youtube.com");
                req.Headers.Add("Accept", "application/xml,text/xml,*/*;q=0.1");
                req.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9"); // Sadece Türkçe
                req.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                req.Headers.Add("DNT", "1");
                req.Headers.Add("Connection", "keep-alive");
                req.Headers.Add("Sec-Fetch-Dest", "empty");
                req.Headers.Add("Sec-Fetch-Mode", "cors");
                req.Headers.Add("Sec-Fetch-Site", "same-origin");
                
                // Timeout ayarla
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                
                var resp = await _httpClient.SendAsync(req, cts.Token);
                
                if (!resp.IsSuccessStatusCode) 
                {
                    _logger.LogDebug($"Video {videoId} Türkçe TimedText başarısız: {resp.StatusCode} - {url}");
                    continue;
                }

                var xml = await resp.Content.ReadAsStringAsync(cts.Token);
                
                if (string.IsNullOrWhiteSpace(xml) || (!xml.Contains("<text") && !xml.Contains("<transcript"))) 
                {
                    continue;
                }

                var list = ParseTimedTextXml(xml, videoId, url);
                
                if (list != null && list.Count > 0) 
                {
                    _logger.LogInformation($"Video {videoId} için TÜRKÇE TimedText transcript bulundu: {list.Count} segment - {url}");
                    return list;
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning($"Video {videoId} Türkçe TimedText timeout: {url}");
            }
            catch (Exception urlEx)
            {
                _logger.LogWarning(urlEx, $"Video {videoId} Türkçe TimedText URL hatası: {url} - {urlEx.Message}");
            }
            
            // Endpoint'ler arasında kısa bekleme
            if (i < candidates.Count - 1)
            {
                await Task.Delay(100); // 100ms bekleme
            }
        }
        
        _logger.LogWarning($"Video {videoId} için tüm Türkçe TimedText endpoint'leri başarısız oldu");
        
        // YouTube transcript API'yi dene
        var transcriptResult = await GetTranscriptViaYouTubeAPI(videoId);
        if (transcriptResult != null && transcriptResult.Count > 0)
        {
            _logger.LogInformation($"Video {videoId} için YouTube API transcript bulundu: {transcriptResult.Count} segment");
            return transcriptResult;
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Türkçe TimedText transcript çekme hatası - Video {videoId}: {ex.Message}");
        return null;
    }
}
        /// <summary>
        /// YouTube transcript API üzerinden transcript çeker
        /// </summary>
        private async Task<List<(TimeSpan Offset, string Text)>?> GetTranscriptViaYouTubeAPI(string videoId)
        {
            try
            {
                if (_youtubeService == null) return null;

                // YouTube transcript API'yi dene
                var transcriptRequest = _youtubeService.Captions.List("snippet", videoId);
                var transcriptResponse = await transcriptRequest.ExecuteAsync();
                
                if (transcriptResponse.Items == null || transcriptResponse.Items.Count == 0)
                {
                    _logger.LogWarning($"YouTube API'de transcript bulunamadı: {videoId}");
                    return null;
                }

                // İlk transcript'i al
                var caption = transcriptResponse.Items.FirstOrDefault();
                if (caption == null) return null;

                // Transcript içeriğini çek
                var downloadRequest = _youtubeService.Captions.Download(caption.Id);
                var transcriptContent = await downloadRequest.ExecuteAsync();
                
                if (string.IsNullOrWhiteSpace(transcriptContent))
                {
                    _logger.LogWarning($"Transcript içeriği boş: {videoId}");
                    return null;
                }

                // SRT formatını parse et
                var segments = ParseSRTTranscript(transcriptContent);
                _logger.LogInformation($"YouTube API transcript başarıyla çekildi: {videoId}");
                return segments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"YouTube API transcript çekme hatası: {videoId}");
                return null;
            }
        }

        /// <summary>
        /// TimedText XML içeriğini parse eder
        /// </summary>
        private List<(TimeSpan Offset, string Text)>? ParseTimedTextXml(string xml, string videoId, string sourceUrl)
        {
            try
            {
                var list = new List<(TimeSpan Offset, string Text)>();
                
                // XML'i parse et
                var xdoc = XDocument.Parse(xml);
                var items = xdoc.Root?.Elements("text");
                
                if (items == null)
                {
                    // Alternatif XML yapısını dene
                    items = xdoc.Descendants("text");
                }
                
                if (items == null)
                {
                    _logger.LogWarning($"Video {videoId} TimedText XML'inde text elementleri bulunamadı: {sourceUrl}");
                    return null;
                }
                
                foreach (var x in items)
                {
                    var startAttr = x.Attribute("start")?.Value;
                    if (string.IsNullOrEmpty(startAttr)) continue;
                    
                    if (!double.TryParse(startAttr, NumberStyles.Any, CultureInfo.InvariantCulture, out var startSec)) 
                    {
                        _logger.LogDebug($"Video {videoId} TimedText geçersiz start zamanı: {startAttr}");
                        continue;
                    }
                    
                    var content = WebUtility.HtmlDecode(x.Value)?.Trim();
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    
                    // İçeriği temizle
                    content = CleanTranscriptText(content);
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    
                    list.Add((TimeSpan.FromSeconds(startSec), content));
                }
                
                _logger.LogInformation($"Video {videoId} TimedText parse tamamlandı: {list.Count} segment - {sourceUrl}");
                return list.Count > 0 ? list : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Video {videoId} TimedText XML parse hatası: {sourceUrl} - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Transcript metnini temizler
        /// </summary>
        private string CleanTranscriptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            
            // HTML entity'lerini decode et
            text = WebUtility.HtmlDecode(text);
            
            // Gereksiz karakterleri temizle
            text = text.Replace("\n", " ")
                      .Replace("\r", " ")
                      .Replace("\t", " ")
                      .Trim();
            
            // Çoklu boşlukları tek boşluğa çevir
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }
            
            return text.Trim();
        }

        /// <summary>
        /// SRT formatındaki transcript'i parse eder
        /// </summary>
        private List<(TimeSpan Offset, string Text)> ParseSRTTranscript(string srtContent)
        {
            var segments = new List<(TimeSpan Offset, string Text)>();
            try
            {
                var lines = srtContent.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // Zaman bilgisini bul
                    if (line.Contains("-->"))
                    {
                        var timeParts = line.Split("-->");
                        if (timeParts.Length == 2)
                        {
                            var startTime = ParseSRTTime(timeParts[0].Trim());
                            var text = "";
                            
                            // Sonraki satırlardan metni al
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                var textLine = lines[j].Trim();
                                if (string.IsNullOrEmpty(textLine)) break;
                                if (textLine.Contains("-->")) break;
                                if (int.TryParse(textLine, out _)) break; // Numara satırı
                                
                                text += textLine + " ";
                            }
                            
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                segments.Add((startTime, text.Trim()));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRT transcript parse hatası");
            }
            
            return segments;
        }

        /// <summary>
        /// SRT zaman formatını parse eder
        /// </summary>
        private TimeSpan ParseSRTTime(string timeString)
        {
            try
            {
                // 00:00:00,000 formatı
                var parts = timeString.Split(':');
                if (parts.Length == 3)
                {
                    var hours = int.Parse(parts[0]);
                    var minutes = int.Parse(parts[1]);
                    var seconds = double.Parse(parts[2].Replace(',', '.'));
                    
                    return TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SRT zaman parse hatası: {timeString}");
            }
            
            return TimeSpan.Zero;
        }

        /// <summary>
        /// Metinden pozisyon türünü tespit eder
        /// </summary>
        private string? PozisyonTuruTespitEt(string metin)
        {
            if (metin.Contains("penaltı") || metin.Contains("penalti") || metin.Contains("penalty"))
                return "Penaltı";
            
            if (metin.Contains("kırmızı kart") || metin.Contains("kirmizi kart") || metin.Contains("red card"))
                return "Kırmızı Kart";
            
            if (metin.Contains("sarı kart") || metin.Contains("sari kart") || metin.Contains("yellow card"))
                return "Sarı Kart";
            
            if (metin.Contains("ofsayt") || metin.Contains("offside"))
                return "Ofsayt";
            
            if (metin.Contains("var"))
                return "VAR";
            
            if (metin.Contains("faul") || metin.Contains("foul"))
                return "Faul";
            
            if (metin.Contains("tartışmalı") || metin.Contains("tartismali") || metin.Contains("controversial"))
                return "Tartışmalı Pozisyon";

            return null;
        }

        // Eski metodlar - geriye uyumluluk için
        public async Task<List<BulunanYorum>> YouTubeHakemYorumlariniAra(string macBilgisi, DateTime macTarihi)
        {
            // Yeni kanal bazlı sistemi kullan
            return await MacIcinKanalYorumlariniTopla(macBilgisi, macTarihi);
        }

        public async Task<List<BulunanYorum>> YouTubeArama(string aramaTerimi, DateTime macTarihi)
        {
            // Yeni kanal bazlı sistemi kullan
            return await MacIcinKanalYorumlariniTopla(aramaTerimi, macTarihi);
        }

        public void Dispose()
        {
            _youtubeService?.Dispose();
            _httpClient?.Dispose();
        }

        /// <summary>
/// HTML scraping ile YouTube altyazılarını çeker
/// </summary>
private async Task<List<(TimeSpan Offset, string Text)>?> GetTranscriptViaHTMLScraping(string videoId)
{
    try
    {
        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
        using var httpClient = new HttpClient();
        
        // Gelişmiş User-Agent ve header'lar (bot algılanmasını önlemek için)
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
        httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        httpClient.DefaultRequestHeaders.Add("DNT", "1");
        httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
        httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        httpClient.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
        
        // Timeout ayarla
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        _logger.LogInformation($"Video {videoId} için HTML scraping başlatılıyor...");
        
        var html = await httpClient.GetStringAsync(videoUrl);
        
        _logger.LogInformation($"Video {videoId} için HTML içeriği alındı, boyut: {html.Length} karakter");
        
        // Güncel YouTube selectors - daha kapsamlı kontrol
        var hasSubtitles = html.Contains("\"captions\":") || 
                          html.Contains("\"captionTracks\":") ||
                          html.Contains("ytp-subtitles-button") ||
                          html.Contains("ytp-caption-window-container") ||
                          html.Contains("caption-window") ||
                          html.Contains("\"hasCaption\":true") ||
                          html.Contains("\"captionsInitialState\":") ||
                          html.Contains("timedtext");
        
        if (hasSubtitles)
        {
            _logger.LogInformation($"Video {videoId} için altyazı bulundu, transcript çekiliyor...");
            
            // Önce captionTracks JSON'ından transcript URL'lerini çıkarmaya çalış
            var transcriptContent = await ExtractTranscriptFromCaptionTracks(html, videoId);
            
            if (string.IsNullOrEmpty(transcriptContent))
            {
                // Alternatif: Direkt timedtext API'sini dene
                transcriptContent = await GetTranscriptFromYouTubePage(videoId);
            }
            
            if (!string.IsNullOrEmpty(transcriptContent))
            {
                var segments = ParseTranscriptContent(transcriptContent);
                _logger.LogInformation($"Video {videoId} için {segments.Count} transcript segmenti bulundu");
                return segments;
            }
        }
        
        _logger.LogWarning($"Video {videoId} için altyazı bulunamadı veya erişilemedi");
        return null;
    }
    catch (HttpRequestException httpEx)
    {
        _logger.LogError(httpEx, $"HTTP isteği hatası - Video {videoId}: {httpEx.Message}");
        return null;
    }
    catch (TaskCanceledException timeoutEx)
    {
        _logger.LogError(timeoutEx, $"Timeout hatası - Video {videoId}: {timeoutEx.Message}");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"HTML scraping transcript hatası - Video {videoId}: {ex.Message}");
        return null;
    }
}

/// <summary>
/// YouTube sayfasından transcript içeriğini çeker
/// </summary>
private async Task<string?> GetTranscriptFromYouTubePage(string videoId)
{
    try
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        
        // Farklı dil ve format kombinasyonlarını dene
        var transcriptUrls = new[]
        {
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&kind=asr&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=tr&kind=asr",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=en&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=en&kind=asr&fmt=srv3",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=en",
            $"https://www.youtube.com/api/timedtext?v={videoId}&lang=en&kind=asr"
        };
        
        foreach (var url in transcriptUrls)
        {
            try
            {
                _logger.LogInformation($"Video {videoId} için transcript URL'si deneniyor: {url}");
                
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(content) && (content.Contains("<text") || content.Contains("<transcript")))
                    {
                        _logger.LogInformation($"Video {videoId} için transcript başarıyla alındı: {url}");
                        return content;
                    }
                }
                
                _logger.LogWarning($"Video {videoId} için transcript URL'si başarısız: {url} - Status: {response.StatusCode}");
            }
            catch (Exception urlEx)
            {
                _logger.LogWarning(urlEx, $"Video {videoId} için transcript URL hatası: {url} - {urlEx.Message}");
            }
            
            // URL'ler arasında kısa bir bekleme
            await Task.Delay(500);
        }
        
        _logger.LogWarning($"Video {videoId} için tüm transcript URL'leri başarısız oldu");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Transcript içeriği çekme hatası - Video {videoId}: {ex.Message}");
        return null;
    }
}

/// <summary>
/// Transcript içeriğini parse eder
/// </summary>
private List<(TimeSpan Offset, string Text)> ParseTranscriptContent(string content)
{
    var segments = new List<(TimeSpan Offset, string Text)>();
    
    try
    {
        // XML format parse et
        var doc = new HtmlDocument();
        doc.LoadHtml(content);
        
        var textNodes = doc.DocumentNode.SelectNodes("//text");
        if (textNodes != null)
        {
            foreach (var node in textNodes)
            {
                var start = node.GetAttributeValue("start", "0");
                var duration = node.GetAttributeValue("dur", "0");
                
                if (double.TryParse(start, out var startSeconds))
                {
                    var offset = TimeSpan.FromSeconds(startSeconds);
                    var text = node.InnerText.Trim();
                    
                    if (!string.IsNullOrEmpty(text))
                    {
                        segments.Add((offset, text));
                    }
                }
            }
        }
        
        _logger.LogInformation($"Transcript'ten {segments.Count} segment parse edildi");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Transcript parse hatası");
    }
    
    return segments;
}

/// <summary>
/// HTML içeriğinden captionTracks JSON'ını parse ederek transcript URL'lerini çıkarır
/// </summary>
private async Task<string?> ExtractTranscriptFromCaptionTracks(string html, string videoId)
{
    try
    {
        // captionTracks JSON'ını bul
        var captionTracksPattern = @"""captionTracks"":\[(.*?)\]";
        var match = System.Text.RegularExpressions.Regex.Match(html, captionTracksPattern);
        
        if (match.Success)
        {
            var captionTracksJson = match.Groups[1].Value;
            _logger.LogInformation($"Video {videoId} için captionTracks bulundu: {captionTracksJson.Substring(0, Math.Min(200, captionTracksJson.Length))}...");
            
            // Türkçe veya İngilizce transcript URL'sini çıkar
            var urlPattern = @"""baseUrl"":""(.*?)""";
            var langPattern = @"""languageCode"":""(tr|en)""";          
            
            var urlMatches = System.Text.RegularExpressions.Regex.Matches(captionTracksJson, urlPattern);
            var langMatches = System.Text.RegularExpressions.Regex.Matches(captionTracksJson, langPattern);
            
            for (int i = 0; i < Math.Min(urlMatches.Count, langMatches.Count); i++)
            {
                var transcriptUrl = urlMatches[i].Groups[1].Value.Replace("\u0026", "&").Replace("\\/", "/");
                var language = langMatches[i].Groups[1].Value;
                
                _logger.LogInformation($"Video {videoId} için {language} transcript URL'si deneniyor: {transcriptUrl}");
                
                var content = await FetchTranscriptFromUrl(transcriptUrl);
                if (!string.IsNullOrEmpty(content))
                {
                    return content;
                }
            }
        }
        
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"CaptionTracks parse hatası - Video {videoId}: {ex.Message}");
        return null;
    }
}

/// <summary>
/// Belirtilen URL'den transcript içeriğini çeker
/// </summary>
private async Task<string?> FetchTranscriptFromUrl(string transcriptUrl)
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://www.youtube.com/");
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        
        var response = await _httpClient.GetAsync(transcriptUrl);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            if (content.Contains("<text") || content.Contains("<transcript"))
            {
                _logger.LogInformation($"Transcript içeriği başarıyla alındı, boyut: {content.Length} karakter");
                return content;
            }
        }
        
        _logger.LogWarning($"Transcript URL'sinden geçersiz içerik alındı: {transcriptUrl}");
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Transcript URL fetch hatası: {transcriptUrl} - {ex.Message}");
        return null;
    }
}
    }
}