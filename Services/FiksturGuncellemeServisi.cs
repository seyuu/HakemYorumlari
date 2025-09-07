using HtmlAgilityPack;
using HakemYorumlari.Data;
using HakemYorumlari.Models;
using Microsoft.EntityFrameworkCore;

namespace HakemYorumlari.Services
{
    public class FiksturGuncellemeServisi
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FiksturGuncellemeServisi> _logger;
        private readonly ApplicationDbContext _context;

        public FiksturGuncellemeServisi(HttpClient httpClient, ILogger<FiksturGuncellemeServisi> logger, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
            
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public async Task<bool> TFFFiksturunuGuncelle()
        {
            try
            {
                _logger.LogInformation("TFF fikstürü güncelleme başlatıldı...");
                
                // Mevcut fikstürü temizle
                await MevcutFiksturuTemizle();
                
                // 34 hafta için fikstürü çek
                for (int hafta = 1; hafta <= 34; hafta++)
                {
                    _logger.LogInformation("Hafta {Hafta} fikstürü çekiliyor...", hafta);
                    
                    var maclar = await HaftaFiksturunuCek(hafta);
                    if (maclar.Any())
                    {
                        await MaclariKaydet(maclar);
                        _logger.LogInformation("Hafta {Hafta}: {MacSayisi} maç kaydedildi", hafta, maclar.Count);
                    }
                    else
                    {
                        _logger.LogWarning("Hafta {Hafta} için maç bulunamadı", hafta);
                    }
                    
                    // Rate limiting için kısa bekleme
                    await Task.Delay(1000);
                }
                
                _logger.LogInformation("TFF fikstürü güncelleme tamamlandı!");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TFF fikstürü güncellenirken hata oluştu");
                return false;
            }
        }

        private async Task MevcutFiksturuTemizle()
        {
            try
            {
                _logger.LogInformation("Mevcut fikstür temizleniyor...");
                
                // İlişkili tabloları temizle
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM KullaniciAnketleri");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM HakemYorumlari");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Pozisyonlar");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Maclar");
                
                _logger.LogInformation("Mevcut fikstür temizlendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fikstür temizlenirken hata oluştu");
                throw;
            }
        }

        private async Task<List<Mac>> HaftaFiksturunuCek(int hafta)
        {
            try
            {
                var url = $"https://www.tff.org/Default.aspx?pageID=198&hafta={hafta}";
                _logger.LogInformation("TFF URL'sine istek gönderiliyor: {Url}", url);
                
                var response = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var maclar = new List<Mac>();
                
                // TFF'de maç bilgileri haftaninMaclariTr class'ında bulunur
                var macSatirlari = doc.DocumentNode.SelectNodes("//tr[@class='haftaninMaclariTr']");
                if (macSatirlari != null)
                {
                    _logger.LogInformation("TFF'te {Count} maç satırı bulundu", macSatirlari.Count);
                    
                    foreach (var satir in macSatirlari)
                    {
                        var mac = MacBilgisiniCikart(satir, hafta);
                        if (mac != null)
                        {
                            maclar.Add(mac);
                        }
                    }
                }
                
                return maclar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hafta {Hafta} fikstürü çekilirken hata oluştu", hafta);
                return new List<Mac>();
            }
        }

        private Mac? MacBilgisiniCikart(HtmlNode satir, int hafta)
        {
            try
            {
                // Ev sahibi takım adını al
                var evSahibiNode = satir.SelectSingleNode(".//td[@class='haftaninMaclariEv']//span");
                var deplasmanNode = satir.SelectSingleNode(".//td[@class='haftaninMaclariDeplasman']//span");
                
                // Tarih bilgisini al
                var tarihNode = satir.SelectSingleNode(".//td[@class='haftaninMaclariTarih']");
                
                // Skor bilgisini al
                var skorSpan1 = satir.SelectSingleNode(".//td[@class='haftaninMaclariSkor']//span[contains(@id, 'Label5')]");
                var skorSpan2 = satir.SelectSingleNode(".//td[@class='haftaninMaclariSkor']//span[contains(@id, 'Label6')]");

                if (evSahibiNode != null && deplasmanNode != null)
                {
                    var evSahibiAdi = evSahibiNode.InnerText?.Trim();
                    var deplasmanAdi = deplasmanNode.InnerText?.Trim();
                    var tarihMetni = tarihNode?.InnerText?.Trim();
                    
                    if (!string.IsNullOrEmpty(evSahibiAdi) && !string.IsNullOrEmpty(deplasmanAdi))
                    {
                        // Tarih parse et - daha güvenli yaklaşım
                        DateTime macTarihi;
                        // Eski kod yerine:
                        var parsedTarih = ParseTFFDate(tarihMetni);
                        if (parsedTarih != DateTime.MinValue)
                        {
                            macTarihi = parsedTarih;
                        }
                        else
                        {
                            _logger.LogWarning("Maç tarihi parse edilemedi: {TarihMetni}, hafta {Hafta} kullanılarak tahmin ediliyor", tarihMetni, hafta);
                            var sezonBaslangici = new DateTime(DateTime.Now.Year, 8, 1);
                            macTarihi = sezonBaslangici.AddDays((hafta - 1) * 7);
                        }
                        
                        // Skor bilgisini al
                        string skor = "-";
                        if (skorSpan1 != null && skorSpan2 != null)
                        {
                            var skor1 = skorSpan1.InnerText?.Trim();
                            var skor2 = skorSpan2.InnerText?.Trim();
                            
                            if (!string.IsNullOrEmpty(skor1) && !string.IsNullOrEmpty(skor2) && 
                                IsValidScore(skor1, skor2))
                            {
                                skor = $"{skor1}-{skor2}";
                            }
                        }
                        
                        var mac = new Mac
                        {
                            EvSahibi = evSahibiAdi,
                            Deplasman = deplasmanAdi,
                            MacTarihi = macTarihi,
                            Hafta = hafta,
                            Liga = "Süper Lig",
                            Skor = skor,
                            Durum = skor == "-" ? MacDurumu.Bekliyor : MacDurumu.Bitti,
                            OtomatikYorumToplamaAktif = true,
                            YorumlarToplandi = false
                        };
                        
                        _logger.LogInformation("Maç bulundu: {EvSahibi} vs {Deplasman} - {Tarih} - {Skor}", 
                            evSahibiAdi, deplasmanAdi, macTarihi.ToString("dd.MM.yyyy"), skor);
                        
                        return mac;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maç bilgisi çıkarılırken hata oluştu");
                return null;
            }
        }

        private DateTime ParseTFFDate(string tarihMetni)
        {
            if (string.IsNullOrEmpty(tarihMetni))
                return DateTime.MinValue;
            
            // Çoklu boşlukları tek boşluğa çevir ve trim yap
            tarihMetni = System.Text.RegularExpressions.Regex.Replace(tarihMetni.Trim(), @"\s+", " ");
            
            // TFF'nin kullandığı farklı tarih formatlarını dene
            string[] formats = {
                "dd.MM.yyyy HH:mm",
                "dd/MM/yyyy HH:mm", 
                "dd.MM.yyyy",
                "dd/MM/yyyy",
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd"
            };
            
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(tarihMetni, format, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }
            
            // Son çare olarak normal TryParse dene
            if (DateTime.TryParse(tarihMetni, out var parsedDate))
                return parsedDate;
            
            return DateTime.MinValue;
        }

        private bool IsValidScore(string skor1, string skor2)
        {
            return int.TryParse(skor1, out _) && int.TryParse(skor2, out _);
        }

        private async Task MaclariKaydet(List<Mac> maclar)
        {
            try
            {
                _context.Maclar.AddRange(maclar);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maçlar kaydedilirken hata oluştu");
                throw;
            }
        }
    }
}
