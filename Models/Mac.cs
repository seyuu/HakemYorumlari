using System.ComponentModel.DataAnnotations;

namespace HakemYorumlari.Models
{
    public class Mac
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string EvSahibi { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string Deplasman { get; set; } = null!;
        
        [Required]
        public DateTime MacTarihi { get; set; }
        
        [StringLength(20)]
        public string Skor { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string Liga { get; set; } = null!;
        
        [Required]
        public int Hafta { get; set; }
        
        // Yeni alanlar
        public MacDurumu Durum { get; set; } = MacDurumu.Bekliyor;
        public bool OtomatikYorumToplamaAktif { get; set; } = true;
        public DateTime? YorumToplamaZamani { get; set; }
        public bool YorumlarToplandi { get; set; } = false;
        public string? YorumToplamaNotlari { get; set; }
        
        public List<Pozisyon> Pozisyonlar { get; set; } = [];
    }
    
    public enum MacDurumu
    {
        Bekliyor,
        Oynaniyor,
        Bitti,
        Ertelendi
    }
}