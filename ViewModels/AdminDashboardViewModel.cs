namespace HakemYorumlari.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalMaclar { get; set; }
        public int TotalPozisyonlar { get; set; }
        public int TotalHakemYorumlari { get; set; }
        public int TotalOylar { get; set; }
        
        // İstatistikler için ek özellikler
        public int BugunkuMaclar { get; set; }
        public int AktifJoblar { get; set; }
        public DateTime? SonGuncelleme { get; set; }
        
        // Yüzde hesaplamaları için
        public double GetPozisyonPerMac()
        {
            if (TotalMaclar == 0) return 0;
            return (double)TotalPozisyonlar / TotalMaclar;
        }
        
        public double GetYorumPerPozisyon()
        {
            if (TotalPozisyonlar == 0) return 0;
            return (double)TotalHakemYorumlari / TotalPozisyonlar;
        }
        
        public double GetOyPerPozisyon()
        {
            if (TotalPozisyonlar == 0) return 0;
            return (double)TotalOylar / TotalPozisyonlar;
        }
    }
}