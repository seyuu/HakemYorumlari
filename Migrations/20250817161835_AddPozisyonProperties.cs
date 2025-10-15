using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HakemYorumlari.Migrations
{
    /// <inheritdoc />
    public partial class AddPozisyonProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HakemKarari",
                table: "Pozisyonlar",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TartismaDerecesi",
                table: "Pozisyonlar",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HakemKarari",
                table: "Pozisyonlar");

            migrationBuilder.DropColumn(
                name: "TartismaDerecesi",
                table: "Pozisyonlar");
        }
    }
}
