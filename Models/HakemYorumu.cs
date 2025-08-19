using System.ComponentModel.DataAnnotations;

namespace HakemYorumlari.Models
{
    public class HakemYorumu
    {
        public int Id { get; set; }
        
        [Required]
        public int PozisyonId { get; set; }
        [Required]
        public Pozisyon Pozisyon { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string YorumcuAdi { get; set; } = null!;
        
        [Required]
        [StringLength(1000)]
        public string Yorum { get; set; } = null!;

        [Required]
        public bool DogruKarar { get; set; } // Yorumcunun görüşü
        
        [Required]
        public DateTime YorumTarihi { get; set; }
        
        [StringLength(100)]
        public string Kanal { get; set; } = null!; // Hangi kanalda yapılan yorum
        
        [StringLength(500)]
        public string? KaynakLink { get; set; } // YouTube, gazete sitesi, TV kanalı web sitesi linki
        
        [StringLength(100)]
        public string? KaynakTuru { get; set; } // "YouTube", "Gazete", "TV_Web", "Sosyal_Medya"
    }
}