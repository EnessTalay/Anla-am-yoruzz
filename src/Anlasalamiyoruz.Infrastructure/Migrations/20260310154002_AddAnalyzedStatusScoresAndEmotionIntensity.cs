using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anlasalamiyoruz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyzedStatusScoresAndEmotionIntensity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Intensity",
                table: "EmotionTags",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LeftScore",
                table: "AnalysisResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RightScore",
                table: "AnalysisResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScoreDescription",
                table: "AnalysisResults",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Intensity",
                table: "EmotionTags");

            migrationBuilder.DropColumn(
                name: "LeftScore",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "RightScore",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "ScoreDescription",
                table: "AnalysisResults");
        }
    }
}
