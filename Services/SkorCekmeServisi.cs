using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace HakemYorumlari.Services
{
    public class SkorCekmeServisi
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SkorCekmeServisi> _logger;

        public SkorCekmeServisi(HttpClient httpClient, ILogger<SkorCekmeServisi> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public async Task<string?> MacSkoruCek(string evSahibi, string deplasman, DateTime macTarihi)
        {
            try
            {
                var hesaplananHafta = GetCurrentWeek(macTarihi);
                _logger.LogInformation("Skor çekiliyor: {EvSahibi} vs {Deplasman} - Tarih: {Tarih} - Hesaplanan Hafta: {Hafta}", 
                    evSahibi, deplasman, macTarihi.ToString("dd.MM.yyyy"), hesaplananHafta);
                
                var skor = await TFFSkoruCek(evSahibi, deplasman, macTarihi);
                if (!string.IsNullOrEmpty(skor))
                {
                    _logger.LogInformation("TFF'den skor bulundu: {Skor}", skor);
                    return skor;
                }

                _logger.LogWarning("TFF'den skor bulunamadı: {EvSahibi} vs {Deplasman}", evSahibi, deplasman);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skor çekilirken hata oluştu: {EvSahibi} vs {Deplasman}", evSahibi, deplasman);
                return null;
            }
        }

        private async Task<string?> TFFSkoruCek(string evSahibi, string deplasman, DateTime macTarihi)
        {
            try
            {
                // Maç tarihine göre hafta hesapla
                var hafta = GetCurrentWeek(macTarihi);
                var url = $"https://www.tff.org/Default.aspx?pageID=198&hafta={hafta}";
                _logger.LogInformation("TFF URL'sine istek gönderiliyor: {Url}", url);
                
                var response = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                _logger.LogDebug("TFF HTML uzunluğu: {Length}", response.Length);
                
                // TFF'de maç bilgileri haftaninMaclariTr class'ında bulunur
                var macSatirlari = doc.DocumentNode.SelectNodes("//tr[@class='haftaninMaclariTr']");
                if (macSatirlari != null)
                {
                    _logger.LogInformation("TFF'te {Count} maç satırı bulundu", macSatirlari.Count);
                    
                    foreach (var satir in macSatirlari)
                    {
                        // Ev sahibi takım adını al
                        var evSahibiNode = satir.SelectSingleNode(".//td[@class='haftaninMaclariEv']//span");
                        var deplasmanNode = satir.SelectSingleNode(".//td[@class='haftaninMaclariDeplasman']//span");
                        // Debug için log ekle
                        if (evSahibiNode != null && deplasmanNode != null)
                        {
                            var evSahibiAdi = evSahibiNode.InnerText?.Trim();
                            var deplasmanAdi = deplasmanNode.InnerText?.Trim();
                            
                            _logger.LogInformation("�� TFF'de bulunan maç: {EvSahibi} vs {Deplasman}", evSahibiAdi, deplasmanAdi);
                        }
                        // Skor bilgisini al - TFF'de skorlar span elementlerinde
                        var skorSpan1 = satir.SelectSingleNode(".//td[@class='haftaninMaclariSkor']//span[contains(@id, 'Label5')]");
                        var skorSpan2 = satir.SelectSingleNode(".//td[@class='haftaninMaclariSkor']//span[contains(@id, 'Label6')]");

                        if (evSahibiNode != null && deplasmanNode != null && skorSpan1 != null && skorSpan2 != null)
                        {
                            var evSahibiAdi = evSahibiNode.InnerText?.Trim();
                            var deplasmanAdi = deplasmanNode.InnerText?.Trim();
                            var skor1 = skorSpan1.InnerText?.Trim();
                            var skor2 = skorSpan2.InnerText?.Trim();

                            if (!string.IsNullOrEmpty(evSahibiAdi) && !string.IsNullOrEmpty(deplasmanAdi) && 
                                !string.IsNullOrEmpty(skor1) && !string.IsNullOrEmpty(skor2))
                            {
                                _logger.LogDebug("TFF'de bulunan maç: {EvSahibi} vs {Deplasman} - Skor: {Skor1}-{Skor2}", evSahibiAdi, deplasmanAdi, skor1, skor2);
                                
                                // Takım isimlerini kontrol et
                                if (IsTeamMatch(evSahibiAdi, evSahibi) && IsTeamMatch(deplasmanAdi, deplasman))
                                {
                                    _logger.LogInformation("Takım eşleşmesi bulundu!");
                                    // Skorları birleştir
                                    if (IsValidScore(skor1, skor2))
                                    {
                                        _logger.LogInformation("Geçerli skor bulundu: {Skor1}-{Skor2}", skor1, skor2);
                                        return $"{skor1}-{skor2}";
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("TFF'de 'haftaninMaclariTr' class'ına sahip maç satırı bulunamadı.");
                }

                _logger.LogWarning("TFF'de maç bulunamadı: {EvSahibi} vs {Deplasman}", evSahibi, deplasman);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TFF'den skor çekilemedi: {EvSahibi} vs {Deplasman}", evSahibi, deplasman);
                return null;
            }
        }

        // Hafta hesaplama metodu
        private int GetCurrentWeek(DateTime macTarihi)
        {
            // AdminController ile aynı algoritma kullan
            var sezonBaslangic = new DateTime(2025, 8, 8);
            
            if (macTarihi < sezonBaslangic)
                return 1;
                
            var gecenGunler = (macTarihi - sezonBaslangic).Days;
            var hafta = (gecenGunler / 7) + 1;
            
            return Math.Min(Math.Max(hafta, 1), 38);
        }

        private bool IsTeamMatch(string tffTeamName, string ourTeamName)
        {
            if (string.IsNullOrEmpty(tffTeamName) || string.IsNullOrEmpty(ourTeamName))
                return false;

            // Daha esnek eşleştirme için takım isimlerini normalize et
            var tffNormalized = NormalizeTeamName(tffTeamName);
            var ourNormalized = NormalizeTeamName(ourTeamName);
            
            // Tam eşleşme
            if (tffNormalized == ourNormalized)
                return true;
                
            // Kısmi eşleşme (en az 4 karakter)
            if (tffNormalized.Length >= 4 && ourNormalized.Length >= 4)
            {
                if (tffNormalized.Contains(ourNormalized) || ourNormalized.Contains(tffNormalized))
                    return true;
            }
            
            // Özel takım eşleştirmeleri
            var teamMappings = new Dictionary<string, string[]>
            {
                { "galatasaray", new[] { "galatasaray", "gs", "gala" } },
                { "fenerbahce", new[] { "fenerbahçe", "fb", "fener" } },
                { "besiktas", new[] { "beşiktaş", "bjk" } },
                { "trabzonspor", new[] { "trabzon", "ts" } }
                // Diğer takımları da ekleyebilirsiniz
            };
            
            foreach (var mapping in teamMappings)
            {
                if (mapping.Value.Any(alias => tffNormalized.Contains(alias) || ourNormalized.Contains(alias)))
                    return true;
            }
            
            return false;
        }

        private string NormalizeTeamName(string teamName)
        {
            return teamName
                .Replace("A.Ş.", "")
                .Replace("FK", "")
                .Replace("FUTBOL KULÜBÜ", "")
                .Replace("KULÜBÜ", "")
                .Replace("SPOR", "")
                .Replace("İSTANBUL", "")
                .Replace("ADANA", "")
                .Replace("DEMİRSPOR", "")
                .Replace("ÇAYKUR", "")
                .Replace("TÜMOSAN", "")
                .Replace("RAMS", "")
                .Replace("İKAS", "")
                .Replace("CORENDON", "")
                .Replace("GÖZTEPE", "")
                .Replace("HESAP.COM", "")
                .Replace("ZECORNER", "")
                .Replace("MISIRLI.COM.TR", "")
                .Trim()
                .ToLowerInvariant()
                .Replace("ı", "i")
                .Replace("ğ", "g")
                .Replace("ü", "u")
                .Replace("ş", "s")
                .Replace("ö", "o")
                .Replace("ç", "c");
        }

        private bool IsValidScore(string skor1, string skor2)
        {
            try
            {
                if (string.IsNullOrEmpty(skor1) || string.IsNullOrEmpty(skor2))
                    return false;
                
                // Skor kontrolü (0-20 arası)
                if (int.TryParse(skor1, out var s1) && int.TryParse(skor2, out var s2) && 
                    s1 >= 0 && s1 <= 20 && s2 >= 0 && s2 <= 20)
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
