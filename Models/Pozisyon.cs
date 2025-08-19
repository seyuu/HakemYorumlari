using System.ComponentModel.DataAnnotations;

namespace HakemYorumlari.Models
{
    public class Pozisyon
    {
        public int Id { get; set; }
        
        [Required]
        public int MacId { get; set; }
        [Required]
        public Mac Mac { get; set; } = null!;
        
        [Required]
        [StringLength(200)]
        public string Aciklama { get; set; } = null!;
        
        [Required]
        public int Dakika { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PozisyonTuru { get; set; } = null!; // Penaltı, Kırmızı Kart, Gol, vs.
        
        [Required]
        public string VideoUrl { get; set; } = null!;
        
        [StringLength(500)]
        public string? EmbedVideoUrl { get; set; } // beIN Sports embed linki
        
        [StringLength(100)]
        public string? VideoKaynagi { get; set; } = "beIN Sports"; // Video kaynağı
        
        // Eksik özellikler eklendi
        [StringLength(100)]
        public string? HakemKarari { get; set; } // Hakemin verdiği karar
        
        [Range(1, 10)]
        public int TartismaDerecesi { get; set; } = 5; // 1-10 arası tartışma derecesi
        
        public List<HakemYorumu> HakemYorumlari { get; set; } = new List<HakemYorumu>();
        public List<KullaniciAnketi> KullaniciAnketleri { get; set; } = new List<KullaniciAnketi>();
    }
}