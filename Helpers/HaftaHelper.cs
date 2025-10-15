namespace HakemYorumlari.Helpers
{
    public static class HaftaHelper
    {
        public static int GetCurrentWeek(DateTime tarih)
        {
            var sezonBaslangic = new DateTime(2024, 8, 9); // 9 Ağustos 2024 Cuma
            const int toplamHafta = 38;

            var diffDays = (tarih.Date - sezonBaslangic.Date).TotalDays;
            var hafta = (int)Math.Floor(diffDays / 7.0) + 1;

            if (hafta < 1) hafta = 1;
            if (hafta > toplamHafta) hafta = toplamHafta;
            return hafta;
        }
        public static (DateTime BaslangicTarihi, DateTime BitisTarihi) GetWeekDateRange(int hafta)
        {
            var sezonBaslangic = new DateTime(2024, 8, 9);
            const int toplamHafta = 38;

            if (hafta < 1) hafta = 1;
            if (hafta > toplamHafta) hafta = toplamHafta;

            var baslangic = sezonBaslangic.AddDays(7 * (hafta - 1)).Date;
            var bitis = baslangic.AddDays(6).Date;
            return (baslangic, bitis);
        }
        public static List<DateTime> GetMatchDaysForWeek(int hafta)
        {
            var (baslangic, bitis) = GetWeekDateRange(hafta);
            var hedefGunler = new[] { DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday };
            var gunler = new List<DateTime>();

            foreach (var gun in hedefGunler)
            {
                var offset = ((int)gun - (int)baslangic.DayOfWeek + 7) % 7;
                var tarih = baslangic.AddDays(offset);
                if (tarih >= baslangic && tarih <= bitis) gunler.Add(tarih);
            }

            if (gunler.Count == 0)
            {
                for (int i = 0; i < 7; i++)
                {
                    gunler.Add(baslangic.AddDays(i));
                }
            }

            return gunler;
        }
        public static double GetSeasonCompletionPercentage(int mevcutHafta)
        {
            const int toplamHafta = 38;
            var clampHafta = Math.Min(Math.Max(mevcutHafta, 0), toplamHafta);
            var yuzde = (clampHafta / (double)toplamHafta) * 100.0;
            return Math.Round(yuzde, 2);
        }
        public static string GetSeasonStatus(int mevcutHafta)
        {
            const int toplamHafta = 38;
            if (mevcutHafta <= 0) return "Başlamadı";
            if (mevcutHafta >= toplamHafta) return "Tamamlandı";
            if (mevcutHafta <= 19) return "İlk Yarı";
            return "İkinci Yarı";
        }
    }
}