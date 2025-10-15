using System.ComponentModel.DataAnnotations;

namespace HakemYorumlari.Models
{
    public class KullaniciAnketi
    {
        public int Id { get; set; }
        
        [Required]
        public int PozisyonId { get; set; }
        [Required]
        public Pozisyon Pozisyon { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string KullaniciIp { get; set; } = null!;

        
        [Required]
        public bool DogruKarar { get; set; } // Kullanıcının oyı
        
        [Required]
        public DateTime OyTarihi { get; set; }
    }
}