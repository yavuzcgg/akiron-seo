using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkironSeo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigrateJsonToNativeJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL refuses an implicit text -> jsonb cast, so each column needs an
            // explicit USING clause. Rows written before this migration can hold an empty
            // string, which is not valid JSON, so those fall back to the entity default.
            AlterTextColumnToJsonb(migrationBuilder, "SeoAudits", "RobotsTxtAiStatusJson", "'{}'");
            AlterTextColumnToJsonb(migrationBuilder, "GeoAnalyses", "RawResponseJson", "'{}'");
            AlterTextColumnToJsonb(migrationBuilder, "GeoAnalyses", "CompetitorsJson", "'[]'");
            AlterTextColumnToJsonb(migrationBuilder, "CrawlResults", "PageSpeedMetricsJson", null);
            AlterTextColumnToJsonb(migrationBuilder, "CrawlResults", "IssuesJson", "'[]'");
            AlterTextColumnToJsonb(migrationBuilder, "CrawlResults", "H1Json", "'[]'");
        }

        /// <param name="emptyFallback">
        /// SQL literal used when the existing value is null or blank. Pass null for a
        /// nullable column so blanks become NULL.
        /// </param>
        private static void AlterTextColumnToJsonb(
            MigrationBuilder migrationBuilder, string table, string column, string emptyFallback)
        {
            var fallback = emptyFallback is null ? "NULL" : $"{emptyFallback}::jsonb";

            migrationBuilder.Sql($"""
                ALTER TABLE "{table}"
                ALTER COLUMN "{column}" TYPE jsonb
                USING CASE
                    WHEN "{column}" IS NULL OR btrim("{column}") = '' THEN {fallback}
                    ELSE "{column}"::jsonb
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // jsonb -> text casts implicitly, so these need no USING clause.
            migrationBuilder.AlterColumn<string>(
                name: "RobotsTxtAiStatusJson",
                table: "SeoAudits",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "RawResponseJson",
                table: "GeoAnalyses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "CompetitorsJson",
                table: "GeoAnalyses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "PageSpeedMetricsJson",
                table: "CrawlResults",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IssuesJson",
                table: "CrawlResults",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "H1Json",
                table: "CrawlResults",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
