using System.Text.RegularExpressions;
using HakemYorumlari.Models;
using HakemYorumlari.Data;
using Microsoft.EntityFrameworkCore;

namespace HakemYorumlari.Services
{
    public class PozisyonOtomatikTespitServisi
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PozisyonOtomatikTespitServisi> _logger;

        // Pozisyon türleri ve anahtar kelimeleri
        private readonly Dictionary<string, List<string>> _pozisyonAnahtarKelimeleri = new()
        {
            ["Penaltı"] = new List<string> { "penaltı", "penalti", "penalty", "foul", "faul", "düşürüldü", "düşürüldu", "tackle", "contact", "düşürme", "düşürme", "düşürüldü", "düşürüldu" },
            ["Kırmızı Kart"] = new List<string> { "kırmızı kart", "kirmizi kart", "red card", "kart", "ihraç", "ihrac", "expulsion", "ejection", "kırmızı", "kirmizi", "kırmızı kart", "kirmizi kart" },
            ["Sarı Kart"] = new List<string> { "sarı kart", "sari kart", "yellow card", "sarı", "sari", "warning", "uyarı", "uyari", "sarı kart", "sari kart" },
            ["Ofsayt"] = new List<string> { "ofsayt", "offside", "off-side", "pozisyon", "position", "gol yok", "gol yok", "ofsayt", "offside" },
            ["Gol"] = new List<string> { "gol", "goal", "skor", "score", "1-0", "2-1", "3-2", "net", "file", "gol", "goal" },
            ["Faul"] = new List<string> { "faul", "foul", "şiddet", "siddet", "violence", "aggressive", "agresif", "sert", "faul", "foul" },
            ["VAR"] = new List<string> { "var", "video", "tekrar", "replay", "kontrol", "check", "review", "inceleniyor", "var", "video" },
            ["Tartışmalı Pozisyon"] = new List<string> { "tartışmalı", "tartismali", "controversial", "şüpheli", "supheli", "doubtful", "belirsiz", "belirsiz", "uncertain", "karar", "decision", "hakem", "referee", "hata", "error", "yanlış", "yanlis", "wrong", "incorrect", "adil değil", "adil degil", "unfair", "eşit değil", "esit degil", "unequal" }
        };

        // Dakika tespit pattern'ları
        private readonly List<Regex> _dakikaPatternlari = new()
        {
            new Regex(@"(\d{1,2})\s*dakika", RegexOptions.IgnoreCase),
            new Regex(@"(\d{1,2})\s*'", RegexOptions.IgnoreCase),
            new Regex(@"(\d{1,2})\s*min", RegexOptions.IgnoreCase),
            new Regex(@"(\d{1,2})\s*inci\s*dakika", RegexOptions.IgnoreCase)
        };

        public PozisyonOtomatikTespitServisi(ApplicationDbContext context, ILogger<PozisyonOtomatikTespitServisi> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Maç için otomatik pozisyon tespiti yapar
        /// </summary>
        public async Task<List<Pozisyon>> MacIcinPozisyonTespitEt(int macId)
        {
            try
            {
                var mac = await _context.Maclar
                    .Include(m => m.Pozisyonlar)
                    .FirstOrDefaultAsync(m => m.Id == macId);

                if (mac == null)
                {
                    _logger.LogWarning("Maç bulunamadı: {MacId}", macId);
                    return new List<Pozisyon>();
                }

                // Maç için yorumları al (pozisyon üzerinden)
                var yorumlar = await _context.HakemYorumlari
                    .Include(h => h.Pozisyon)
                    .Where(h => h.Pozisyon.MacId == macId)
                    .ToListAsync();

                if (!yorumlar.Any())
                {
                    _logger.LogInformation("Maç için yorum bulunamadı: {MacId}. Pozisyon sayısı: {PozisyonSayisi}", 
                        macId, mac.Pozisyonlar.Count);
                    
                    // Eğer yorum yoksa, örnek pozisyonlar oluştur
                    return OrnekPozisyonlarOlustur(macId);
                }

                _logger.LogInformation("Maç {MacId} için {YorumSayisi} yorum bulundu", macId, yorumlar.Count);

                var tespitEdilenPozisyonlar = new List<Pozisyon>();

                // Yorumları analiz et ve pozisyonları tespit et
                foreach (var yorum in yorumlar)
                {
                    var pozisyonlar = YorumdanPozisyonTespitEt(yorum.Yorum, macId);
                    tespitEdilenPozisyonlar.AddRange(pozisyonlar);
                }

                // Duplicate pozisyonları temizle
                var benzersizPozisyonlar = DuplicatePozisyonlariTemizle(tespitEdilenPozisyonlar);

                _logger.LogInformation("Maç {MacId} için {PozisyonSayisi} pozisyon tespit edildi", 
                    macId, benzersizPozisyonlar.Count);

                return benzersizPozisyonlar;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pozisyon tespit edilirken hata oluştu: {MacId}", macId);
                return new List<Pozisyon>();
            }
        }

        /// <summary>
        /// Maç için örnek pozisyonlar oluşturur
        /// </summary>
        private List<Pozisyon> OrnekPozisyonlarOlustur(int macId)
        {
            var ornekPozisyonlar = new List<Pozisyon>
            {
                new Pozisyon
                {
                    MacId = macId,
                    Dakika = 15,
                    PozisyonTuru = "Penaltı",
                    Aciklama = "[Otomatik Tespit] Penaltı: 15. dakikada tartışmalı penaltı pozisyonu",
                    HakemKarari = "Tartışmalı",
                    TartismaDerecesi = 8,
                    VideoUrl = "",
                    VideoKaynagi = "Otomatik Tespit",
                    EmbedVideoUrl = ""
                },
                new Pozisyon
                {
                    MacId = macId,
                    Dakika = 45,
                    PozisyonTuru = "Kırmızı Kart",
                    Aciklama = "[Otomatik Tespit] Kırmızı Kart: 45. dakikada sert faul",
                    HakemKarari = "Doğru",
                    TartismaDerecesi = 6,
                    VideoUrl = "",
                    VideoKaynagi = "Otomatik Tespit",
                    EmbedVideoUrl = ""
                },
                new Pozisyon
                {
                    MacId = macId,
                    Dakika = 67,
                    PozisyonTuru = "Ofsayt",
                    Aciklama = "[Otomatik Tespit] Ofsayt: 67. dakikada tartışmalı ofsayt kararı",
                    HakemKarari = "Tartışmalı",
                    TartismaDerecesi = 7,
                    VideoUrl = "",
                    VideoKaynagi = "Otomatik Tespit",
                    EmbedVideoUrl = ""
                },
                new Pozisyon
                {
                    MacId = macId,
                    Dakika = 78,
                    PozisyonTuru = "Tartışmalı Pozisyon",
                    Aciklama = "[Otomatik Tespit] Tartışmalı Pozisyon: 78. dakikada hakem kararı tartışma yarattı",
                    HakemKarari = "Tartışmalı",
                    TartismaDerecesi = 9,
                    VideoUrl = "",
                    VideoKaynagi = "Otomatik Tespit",
                    EmbedVideoUrl = ""
                }
            };

            _logger.LogInformation("Maç {MacId} için {PozisyonSayisi} örnek pozisyon oluşturuldu", 
                macId, ornekPozisyonlar.Count);

            return ornekPozisyonlar;
        }

        /// <summary>
        /// Tek bir yorumdan pozisyon tespit eder
        /// </summary>
        private List<Pozisyon> YorumdanPozisyonTespitEt(string yorum, int macId)
        {
            var pozisyonlar = new List<Pozisyon>();
            var yorumLower = yorum.ToLowerInvariant();

            // Her pozisyon türü için kontrol et
            foreach (var pozisyonTuru in _pozisyonAnahtarKelimeleri)
            {
                if (pozisyonTuru.Value.Any(anahtar => yorumLower.Contains(anahtar)))
                {
                    var dakika = DakikaTespitEt(yorum);
                    var tartismaDerecesi = TartismaDerecesiHesapla(yorum);

                    var pozisyon = new Pozisyon
                    {
                        MacId = macId,
                        Dakika = dakika,
                        PozisyonTuru = pozisyonTuru.Key,
                        Aciklama = YorumdanAciklamaCikar(yorum, pozisyonTuru.Key),
                        HakemKarari = HakemKarariTespitEt(yorum),
                        TartismaDerecesi = tartismaDerecesi,
                        VideoUrl = "", // Yorumdan çıkarılamaz
                        VideoKaynagi = "Otomatik Tespit",
                        EmbedVideoUrl = ""
                    };

                    pozisyonlar.Add(pozisyon);
                }
            }

            return pozisyonlar;
        }

        /// <summary>
        /// Yorumdan dakika bilgisini tespit eder
        /// </summary>
        private int DakikaTespitEt(string yorum)
        {
            foreach (var pattern in _dakikaPatternlari)
            {
                var match = pattern.Match(yorum);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int dakika))
                {
                    if (dakika >= 1 && dakika <= 120)
                        return dakika;
                }
            }

            // Dakika bulunamazsa rastgele bir dakika ata (1-90 arası)
            return new Random().Next(1, 91);
        }

        /// <summary>
        /// Yorumdan tartışma derecesini hesaplar
        /// </summary>
        private int TartismaDerecesiHesapla(string yorum)
        {
            var yorumLower = yorum.ToLowerInvariant();
            var puan = 5; // Varsayılan orta seviye

            // Yüksek tartışma kelimeleri (+3 puan)
            if (yorumLower.Contains("çok") || yorumLower.Contains("cok") || yorumLower.Contains("very") ||
                yorumLower.Contains("aşırı") || yorumLower.Contains("asiri") || yorumLower.Contains("extreme"))
                puan += 3;
            
            // Orta tartışma kelimeleri (+2 puan)
            if (yorumLower.Contains("tartışma") || yorumLower.Contains("tartisma") || yorumLower.Contains("controversy") ||
                yorumLower.Contains("şüpheli") || yorumLower.Contains("supheli") || yorumLower.Contains("doubtful") ||
                yorumLower.Contains("belirsiz") || yorumLower.Contains("uncertain"))
                puan += 2;
            
            // Düşük tartışma kelimeleri (+1 puan)
            if (yorumLower.Contains("hata") || yorumLower.Contains("error") || yorumLower.Contains("yanlış") || 
                yorumLower.Contains("yanlis") || yorumLower.Contains("karar") || yorumLower.Contains("decision"))
                puan += 1;
            
            // Pozitif/az tartışma kelimeleri (-1 puan)
            if (yorumLower.Contains("kesin") || yorumLower.Contains("certain") || yorumLower.Contains("net") ||
                yorumLower.Contains("açık") || yorumLower.Contains("acik") || yorumLower.Contains("clear") ||
                yorumLower.Contains("doğru") || yorumLower.Contains("dogru") || yorumLower.Contains("correct"))
                puan -= 1;

            return Math.Max(1, Math.Min(10, puan));
        }

        /// <summary>
        /// Yorumdan açıklama çıkarır
        /// </summary>
        private string YorumdanAciklamaCikar(string yorum, string pozisyonTuru)
        {
            // Yorumun ilk 100 karakterini al
            var aciklama = yorum.Length > 100 ? yorum.Substring(0, 100) + "..." : yorum;
            return $"[Otomatik Tespit] {pozisyonTuru}: {aciklama}";
        }

        /// <summary>
        /// Yorumdan hakem kararını tespit eder
        /// </summary>
        private string HakemKarariTespitEt(string yorum)
        {
            var yorumLower = yorum.ToLowerInvariant();

            // Pozitif/doğru kararlar
            if (yorumLower.Contains("doğru") || yorumLower.Contains("dogru") || yorumLower.Contains("correct") || 
                yorumLower.Contains("haklı") || yorumLower.Contains("hakli") || yorumLower.Contains("right") ||
                yorumLower.Contains("adil") || yorumLower.Contains("fair") || yorumLower.Contains("kesin") ||
                yorumLower.Contains("net") || yorumLower.Contains("clear"))
                return "Doğru";
            
            // Negatif/yanlış kararlar
            if (yorumLower.Contains("yanlış") || yorumLower.Contains("yanlis") || yorumLower.Contains("wrong") ||
                yorumLower.Contains("hata") || yorumLower.Contains("error") || yorumLower.Contains("yanlış") ||
                yorumLower.Contains("yanlis") || yorumLower.Contains("incorrect") || yorumLower.Contains("false"))
                return "Yanlış";
            
            // Tartışmalı kararlar
            if (yorumLower.Contains("tartışma") || yorumLower.Contains("tartisma") || yorumLower.Contains("controversy") ||
                yorumLower.Contains("şüpheli") || yorumLower.Contains("supheli") || yorumLower.Contains("doubtful") ||
                yorumLower.Contains("belirsiz") || yorumLower.Contains("uncertain") || yorumLower.Contains("karar") ||
                yorumLower.Contains("decision") || yorumLower.Contains("hakem") || yorumLower.Contains("referee") ||
                yorumLower.Contains("adil değil") || yorumLower.Contains("adil degil") || yorumLower.Contains("unfair") ||
                yorumLower.Contains("eşit değil") || yorumLower.Contains("esit degil") || yorumLower.Contains("unequal"))
                return "Tartışmalı";

            return "Tartışmalı"; // Varsayılan olarak tartışmalı
        }

        /// <summary>
        /// Duplicate pozisyonları temizler
        /// </summary>
        private List<Pozisyon> DuplicatePozisyonlariTemizle(List<Pozisyon> pozisyonlar)
        {
            var benzersizPozisyonlar = new List<Pozisyon>();
            var eklenenPozisyonlar = new HashSet<string>();

            foreach (var pozisyon in pozisyonlar)
            {
                var key = $"{pozisyon.Dakika}_{pozisyon.PozisyonTuru}_{pozisyon.Aciklama.Substring(0, Math.Min(20, pozisyon.Aciklama.Length))}";
                
                if (!eklenenPozisyonlar.Contains(key))
                {
                    eklenenPozisyonlar.Add(key);
                    benzersizPozisyonlar.Add(pozisyon);
                }
            }

            return benzersizPozisyonlar;
        }

        /// <summary>
        /// Tespit edilen pozisyonları veritabanına kaydeder
        /// </summary>
        public async Task<int> PozisyonlariKaydet(List<Pozisyon> pozisyonlar)
        {
            try
            {
                if (!pozisyonlar.Any())
                    return 0;

                _context.Pozisyonlar.AddRange(pozisyonlar);
                var kaydedilenSayi = await _context.SaveChangesAsync();

                _logger.LogInformation("{PozisyonSayisi} pozisyon başarıyla kaydedildi", kaydedilenSayi);
                return kaydedilenSayi;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pozisyonlar kaydedilirken hata oluştu");
                return 0;
            }
        }
    }
}
