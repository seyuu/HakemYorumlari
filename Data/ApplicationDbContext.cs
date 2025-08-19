using Microsoft.EntityFrameworkCore;
using HakemYorumlari.Models;

namespace HakemYorumlari.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Mac> Maclar { get; set; }
        public DbSet<Pozisyon> Pozisyonlar { get; set; }
        public DbSet<HakemYorumu> HakemYorumlari { get; set; }
        public DbSet<KullaniciAnketi> KullaniciAnketleri { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mac - Pozisyon ilişkisi
            modelBuilder.Entity<Pozisyon>()
                .HasOne(p => p.Mac)
                .WithMany(m => m.Pozisyonlar)
                .HasForeignKey(p => p.MacId)
                .OnDelete(DeleteBehavior.Cascade);

            // Pozisyon - HakemYorumu ilişkisi
            modelBuilder.Entity<HakemYorumu>()
                .HasOne(h => h.Pozisyon)
                .WithMany(p => p.HakemYorumlari)
                .HasForeignKey(h => h.PozisyonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Pozisyon - KullaniciAnketi ilişkisi
            modelBuilder.Entity<KullaniciAnketi>()
                .HasOne(k => k.Pozisyon)
                .WithMany(p => p.KullaniciAnketleri)
                .HasForeignKey(k => k.PozisyonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bir IP'den bir pozisyon için sadece bir oy
            modelBuilder.Entity<KullaniciAnketi>()
                .HasIndex(k => new { k.PozisyonId, k.KullaniciIp })
                .IsUnique();
        }
    }
}