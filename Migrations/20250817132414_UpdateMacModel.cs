using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakemYorumlari.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMacModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmbedVideoUrl",
                table: "Pozisyonlar",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoKaynagi",
                table: "Pozisyonlar",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Durum",
                table: "Maclar",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "OtomatikYorumToplamaAktif",
                table: "Maclar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "YorumToplamaNotlari",
                table: "Maclar",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "YorumToplamaZamani",
                table: "Maclar",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "YorumlarToplandi",
                table: "Maclar",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KaynakLink",
                table: "HakemYorumlari",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KaynakTuru",
                table: "HakemYorumlari",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbedVideoUrl",
                table: "Pozisyonlar");

            migrationBuilder.DropColumn(
                name: "VideoKaynagi",
                table: "Pozisyonlar");

            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Maclar");

            migrationBuilder.DropColumn(
                name: "OtomatikYorumToplamaAktif",
                table: "Maclar");

            migrationBuilder.DropColumn(
                name: "YorumToplamaNotlari",
                table: "Maclar");

            migrationBuilder.DropColumn(
                name: "YorumToplamaZamani",
                table: "Maclar");

            migrationBuilder.DropColumn(
                name: "YorumlarToplandi",
                table: "Maclar");

            migrationBuilder.DropColumn(
                name: "KaynakLink",
                table: "HakemYorumlari");

            migrationBuilder.DropColumn(
                name: "KaynakTuru",
                table: "HakemYorumlari");
        }
    }
}
