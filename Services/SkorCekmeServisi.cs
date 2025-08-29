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
                _logger.LogInformation("Skor çekiliyor: {EvSahibi} vs {Deplasman} - {Tarih}", evSahibi, deplasman, macTarihi.ToString("dd.MM.yyyy"));
                
                // Sadece TFF'den dene
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
            // TFF'deki hafta sistemi:
            // 08.08.2025 = 1. hafta
            // 15.08.2025 = 2. hafta
            // 22.08.2025 = 3. hafta
            // vs.
            
            // Süper Lig sezon başlangıcı (8 Ağustos 2025)
            var sezonBaslangici = new DateTime(2025, 8, 8);
            
            // Maç tarihi ile sezon başlangıcı arasındaki fark
            var fark = macTarihi - sezonBaslangici;
            
            // Hafta hesaplama (7 gün = 1 hafta)
            var hafta = (int)Math.Ceiling(fark.TotalDays / 7.0);
            
            // Minimum 1. hafta olsun
            return Math.Max(1, hafta);
        }

        private bool IsTeamMatch(string tffTeamName, string ourTeamName)
        {
            if (string.IsNullOrEmpty(tffTeamName) || string.IsNullOrEmpty(ourTeamName))
                return false;

            // TFF'deki isimleri temizle
            var tffClean = tffTeamName
                .Replace("A.Ş.", "")
                .Replace("A.Ş", "")
                .Replace("FUTBOL KULÜBÜ", "")
                .Replace("FK", "")
                .Replace("FUTBOL", "")
                .Replace("KULÜBÜ", "")
                .Replace("KULUBU", "")
                .Replace("COR", "")
                .Replace("ENDON", "")
                .Replace("HESAP.COM", "")
                .Replace("ZECORNER", "")
                .Replace("TÜMOSAN", "")
                .Replace("IKAS", "")
                .Replace("RAMS", "")
                .Replace("ÇAYKUR", "")
                .Replace("ADANA", "")
                .Replace("DEMİRSPOR", "")
                .Replace("İSTANBUL", "")
                .Trim();
            
            var ourClean = ourTeamName
                .Replace("A.Ş.", "")
                .Replace("FK", "")
                .Replace("İSTANBUL", "")
                .Trim();

            // Debug için detaylı log
            _logger.LogInformation("🔍 Takım eşleştirme detayı:");
            _logger.LogInformation("   TFF orijinal: '{TffOriginal}'", tffTeamName);
            _logger.LogInformation("   TFF temizlenmiş: '{TffClean}'", tffClean);
            _logger.LogInformation("   Bizim orijinal: '{OurOriginal}'", ourTeamName);
            _logger.LogInformation("   Bizim temizlenmiş: '{OurClean}'", ourClean);

            // Eşleştirme kontrolü
            if (tffClean.Contains(ourClean, StringComparison.OrdinalIgnoreCase) || 
                ourClean.Contains(tffClean, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("✅ Takım eşleşmesi bulundu!");
                return true;
            }

            _logger.LogWarning("❌ Takım eşleşmesi bulunamadı!");
            return false;
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