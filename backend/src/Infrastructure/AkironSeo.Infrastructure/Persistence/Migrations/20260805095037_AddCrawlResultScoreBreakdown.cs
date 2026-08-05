using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkironSeo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrawlResultScoreBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "[]" rather than the scaffolded "": an empty string is not valid JSON and
            // PostgreSQL rejects it as a jsonb default. Rows crawled before this column
            // existed get an empty array, and the API reports no breakdown for them.
            migrationBuilder.AddColumn<string>(
                name: "ScoreBreakdownJson",
                table: "CrawlResults",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoreBreakdownJson",
                table: "CrawlResults");
        }
    }
}
