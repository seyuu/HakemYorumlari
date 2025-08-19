using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakemYorumlari.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Maclar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvSahibi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Deplasman = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MacTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Skor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Liga = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Hafta = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maclar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pozisyonlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MacId = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Dakika = table.Column<int>(type: "int", nullable: false),
                    PozisyonTuru = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pozisyonlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pozisyonlar_Maclar_MacId",
                        column: x => x.MacId,
                        principalTable: "Maclar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HakemYorumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PozisyonId = table.Column<int>(type: "int", nullable: false),
                    YorumcuAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Yorum = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DogruKarar = table.Column<bool>(type: "bit", nullable: false),
                    YorumTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kanal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HakemYorumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HakemYorumlari_Pozisyonlar_PozisyonId",
                        column: x => x.PozisyonId,
                        principalTable: "Pozisyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KullaniciAnketleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PozisyonId = table.Column<int>(type: "int", nullable: false),
                    KullaniciIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DogruKarar = table.Column<bool>(type: "bit", nullable: false),
                    OyTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KullaniciAnketleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KullaniciAnketleri_Pozisyonlar_PozisyonId",
                        column: x => x.PozisyonId,
                        principalTable: "Pozisyonlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HakemYorumlari_PozisyonId",
                table: "HakemYorumlari",
                column: "PozisyonId");

            migrationBuilder.CreateIndex(
                name: "IX_KullaniciAnketleri_PozisyonId_KullaniciIp",
                table: "KullaniciAnketleri",
                columns: new[] { "PozisyonId", "KullaniciIp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pozisyonlar_MacId",
                table: "Pozisyonlar",
                column: "MacId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HakemYorumlari");

            migrationBuilder.DropTable(
                name: "KullaniciAnketleri");

            migrationBuilder.DropTable(
                name: "Pozisyonlar");

            migrationBuilder.DropTable(
                name: "Maclar");
        }
    }
}
