using HakemYorumlari.Models;
using System.Text.RegularExpressions;

namespace HakemYorumlari.Services
{
    public class BeINSportsEmbedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BeINSportsEmbedService> _logger;
        
        public BeINSportsEmbedService(HttpClient httpClient, ILogger<BeINSportsEmbedService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        
        public async Task<string?> GetBeINSportsEmbedUrl(Mac mac, string pozisyonTuru, int dakika)
        {
            try
            {
                // beIN Sports'un özel embed URL'lerini oluştur
                var matchSlug = GenerateMatchSlug(mac);
                var dateSlug = mac.MacTarihi.ToString("yyyy-MM-dd");
                
                // Pozisyon bazlı embed URL'ler
                var embedUrl = pozisyonTuru.ToLower() switch
                {
                    "penaltı" => $"https://embed.beinsports.com.tr/highlights/{dateSlug}/{matchSlug}/penalty/{dakika}",
                    "kırmızı kart" => $"https://embed.beinsports.com.tr/highlights/{dateSlug}/{matchSlug}/red-card/{dakika}",
                    "var" => $"https://embed.beinsports.com.tr/highlights/{dateSlug}/{matchSlug}/var/{dakika}",
                    "ofsayt" => $"https://embed.beinsports.com.tr/highlights/{dateSlug}/{matchSlug}/offside/{dakika}",
                    "faul" => $"https://embed.beinsports.com.tr/highlights/{dateSlug}/{matchSlug}/foul/{dakika}",
                    _ => $"https://embed.beinsports.com.tr/highlights/{dateSlug}/{matchSlug}/highlight/{dakika}"
                };
                
                // URL'nin geçerliliğini kontrol et
                if (await IsValidEmbedUrl(embedUrl))
                {
                    return embedUrl;
                }
                
                // Alternatif URL formatları dene
                return await TryAlternativeFormats(mac, pozisyonTuru, dakika);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"beIN Sports embed URL oluşturma hatası: {mac.EvSahibi} vs {mac.Deplasman}");
                return null;
            }
        }
        
        public async Task<List<string>> GetMatchHighlights(Mac mac)
        {
            var highlights = new List<string>();
            
            try
            {
                var matchSlug = GenerateMatchSlug(mac);
                var dateSlug = mac.MacTarihi.ToString("yyyy-MM-dd");
                
                // Maç özetleri için farklı formatları dene
                var possibleUrls = new[]
                {
                    $"https://embed.beinsports.com.tr/match/{dateSlug}/{matchSlug}/highlights",
                    $"https://embed.beinsports.com.tr/highlights/{dateSlug}/{matchSlug}/full",
                    $"https://embed.beinsports.com.tr/video/{dateSlug}/{matchSlug}"
                };
                
                foreach (var url in possibleUrls)
                {
                    if (await IsValidEmbedUrl(url))
                    {
                        highlights.Add(url);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Maç özetleri alınırken hata: {mac.EvSahibi} vs {mac.Deplasman}");
            }
            
            return highlights;
        }
        
        private string GenerateMatchSlug(Mac mac)
        {
            var evSahibi = CleanTeamName(mac.EvSahibi);
            var deplasman = CleanTeamName(mac.Deplasman);
            return $"{evSahibi}-vs-{deplasman}";
        }
        
        private string CleanTeamName(string teamName)
        {
            // Takım adını URL-friendly hale getir
            return Regex.Replace(teamName.ToLower()
                .Replace("ş", "s")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ç", "c")
                .Replace("ı", "i")
                .Replace(" ", "-"), @"[^a-z0-9-]", "");
        }
        
        private async Task<bool> IsValidEmbedUrl(string url)
        {
            try
            {
                var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        private async Task<string?> TryAlternativeFormats(Mac mac, string pozisyonTuru, int dakika)
        {
            var matchSlug = GenerateMatchSlug(mac);
            var dateSlug = mac.MacTarihi.ToString("yyyy-MM-dd");
            
            // Alternatif URL formatları
            var alternatives = new[]
            {
                $"https://embed.beinsports.com.tr/video/{dateSlug}/{matchSlug}/{pozisyonTuru.ToLower()}/{dakika}",
                $"https://embed.beinsports.com.tr/clip/{dateSlug}/{matchSlug}/{dakika}",
                $"https://embed.beinsports.com.tr/moment/{matchSlug}/{dakika}"
            };
            
            foreach (var url in alternatives)
            {
                if (await IsValidEmbedUrl(url))
                {
                    return url;
                }
            }
            
            return null;
        }
    }
}